using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SupportTools.Tests.Registry.Adapters;

//an in-memory SupportToolsServer for the registry routes of SupportToolsServerApiClient, following the server's
//CLAUDE.md (Registry conventions). Records match by name ignoring case and keep the spelling of the last write.
//- registry areas (environments, …): GET lists the records ordered by name; POST update/{key} is the upsert whose body
//  Version is the expected version (0 creates; 409 ConcurrencyConflict when it differs; 404 RecordWithNameNotFound
//  for a missing record with N ≠ 0) and answers the new version; DELETE delete/{key}?version=N;
//- singletons (settings/global, settings/projectcreator): GET answers an empty record with Version 0 before the first
//  create, POST update follows the upsert rules;
//- the old git endpoints check no version: updategitrepo/{key} and syncup…/{merge} add or replace a record and
//  increment its version; the deletes are unconditional and answer 404 with their own code for a missing record.
//Every request is logged as "METHOD path?query"
internal sealed class FakeSupportToolsServer : HttpMessageHandler
{
    public const string Address = "http://127.0.0.1:0/api/v1";
    public const string GitRepos = "gitrepos";
    public const string GitIgnoreFileTypes = "gitignorefiletypes";
    public const string EditorConfigFileTypes = "editorconfigfiletypes";
    public const string GlobalSettings = "settings/global";
    public const string ProjectCreatorSettings = "settings/projectcreator";

    private const string ApiBase = "/api/v1/";
    private const string VersionProperty = "Version";

    private static readonly HashSet<string> RegistryAreas = new(StringComparer.Ordinal)
    {
        "environments",
        "runtimes",
        "npmpackages",
        "reactapptemplates",
        "dotnettools",
        "smartschemas",
        "filestorages",
        "apiclients",
        "databaseserverconnections",
        "servers",
        "projecttemplates"
    };

    private readonly Dictionary<string, Dictionary<string, JObject>> _areas = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (HttpStatusCode StatusCode, string Code)> _failures =
        new(StringComparer.Ordinal);

    public List<string> Requests { get; } = [];

    //every request fails like a server that cannot be reached
    public bool IsUnavailable { get; set; }

    //runs before a request is answered, with "METHOD path": another computer that changes the server in between
    public Action<string>? BeforeRequest { get; set; }

    //the stored records of an area, by name ignoring case. A singleton is stored under the empty name
    public Dictionary<string, JObject> Records(string area)
    {
        if (!_areas.TryGetValue(area, out Dictionary<string, JObject>? records))
        {
            records = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            _areas.Add(area, records);
        }

        return records;
    }

    //stores a record as if the server had saved it with this version
    public void Store(string area, string name, object record, int version)
    {
        JObject json = JObject.FromObject(record);
        json[VersionProperty] = version;
        Records(area).Remove(name);
        Records(area)[name] = json;
    }

    public T? Get<T>(string area, string name)
    {
        return Records(area).TryGetValue(name, out JObject? record) ? record.ToObject<T>() : default;
    }

    public int VersionOf(string area, string name)
    {
        return Records(area).TryGetValue(name, out JObject? record) ? record.Value<int>(VersionProperty) : 0;
    }

    //answers this request ("METHOD path") with a ProblemDetails error instead of handling it
    public void Fail(string request, HttpStatusCode statusCode, string code)
    {
        _failures[request] = (statusCode, code);
    }

    public int RequestCount(string method, string pathPart)
    {
        return Requests.Count(x =>
            x.StartsWith($"{method} ", StringComparison.Ordinal) && x.Contains(pathPart, StringComparison.Ordinal));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Uri uri = request.RequestUri!;
        Requests.Add($"{request.Method} {uri.AbsolutePath}{uri.Query}");
        if (IsUnavailable)
        {
            throw new HttpRequestException("Connection refused (fake server)");
        }

        string requestKey = $"{request.Method} {uri.AbsolutePath}";
        BeforeRequest?.Invoke(requestKey);

        string body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        string[] segments = [.. uri.AbsolutePath[ApiBase.Length..].Split('/').Select(Uri.UnescapeDataString)];
        HttpResponseMessage response = Respond(requestKey, request.Method, segments, body, uri.Query);
        response.RequestMessage = request;
        return response;
    }

    private HttpResponseMessage Respond(string requestKey, HttpMethod method, string[] segments, string body,
        string query)
    {
        if (_failures.TryGetValue(requestKey, out (HttpStatusCode StatusCode, string Code) failure))
        {
            return Problem(failure.StatusCode, failure.Code);
        }

        return Route(method, segments, body, query);
    }

    private HttpResponseMessage Route(HttpMethod method, string[] segments, string body, string query)
    {
        if (RegistryAreas.Contains(segments[0]))
        {
            return RouteRegistryArea(method, segments, body, query);
        }

        if (segments[0] == "settings")
        {
            string area = $"settings/{segments[1]}";
            return method == HttpMethod.Get ? GetSingleton(area) : Upsert(area, "Name", string.Empty, body);
        }

        return segments[1] switch
        {
            "gitrepos" => List(GitRepos, "GitProjectName"),
            "updategitrepo" => WriteWithoutVersion(GitRepos, "GitProjectName", segments[2], JObject.Parse(body)),
            "deletegitrepo" => DeleteWithoutVersion(GitRepos, segments[2], "GitWithKeyNotFound"),
            "gitignorefiletypeslist" => List(GitIgnoreFileTypes, "Name"),
            "syncupgitignorefiletypes" => SyncUp(GitIgnoreFileTypes, body, true),
            "deletegitignorefiletype" => DeleteWithoutVersion(GitIgnoreFileTypes, segments[2],
                "GitIgnoreFileTypeWithNameNotFound"),
            "editorconfigfiletypeslist" => List(EditorConfigFileTypes, "Name"),
            "syncupeditorconfigfiletypes" => SyncUp(EditorConfigFileTypes, body, false),
            "deleteeditorconfigfiletype" => DeleteWithoutVersion(EditorConfigFileTypes, segments[2],
                "EditorConfigFileTypeWithNameNotFound"),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }

    private HttpResponseMessage RouteRegistryArea(HttpMethod method, string[] segments, string body, string query)
    {
        string area = segments[0];
        if (method == HttpMethod.Get)
        {
            return List(area, "Name");
        }

        if (method == HttpMethod.Post)
        {
            return Upsert(area, "Name", segments[2], body);
        }

        //?version=N&apikey=… (no key in the tests)
        string? version = query.TrimStart('?').Split('&').Select(x => x.Split('='))
            .Where(x => x[0] == "version").Select(x => x[1]).FirstOrDefault();
        return Delete(area, segments[2], version is null ? null : int.Parse(version, CultureInfo.InvariantCulture));
    }

    private HttpResponseMessage List(string area, string nameProperty)
    {
        var records = new JArray(Records(area).Values
            .OrderBy(x => x.Value<string>(nameProperty), StringComparer.OrdinalIgnoreCase).Select(x => x.DeepClone()));
        return Json(records.ToString(Formatting.None));
    }

    private HttpResponseMessage GetSingleton(string area)
    {
        return Records(area).TryGetValue(string.Empty, out JObject? stored)
            ? Json(stored.ToString(Formatting.None))
            : Json("{\"Version\":0}");
    }

    private HttpResponseMessage Upsert(string area, string nameProperty, string key, string body)
    {
        JObject record = JObject.Parse(body);
        int expectedVersion = record.Value<int?>(VersionProperty) ?? 0;
        Dictionary<string, JObject> records = Records(area);
        if (!records.TryGetValue(key, out JObject? stored))
        {
            if (expectedVersion != 0)
            {
                return Problem(HttpStatusCode.NotFound, "RecordWithNameNotFound");
            }
        }
        else if (stored.Value<int>(VersionProperty) != expectedVersion)
        {
            return Problem(HttpStatusCode.Conflict, "ConcurrencyConflict");
        }

        int newVersion = expectedVersion + 1;
        if (key.Length > 0)
        {
            record[nameProperty] = key;
        }

        record[VersionProperty] = newVersion;
        records.Remove(key);
        records[key] = record;
        return Json(newVersion.ToString(CultureInfo.InvariantCulture));
    }

    private HttpResponseMessage Delete(string area, string key, int? version)
    {
        Dictionary<string, JObject> records = Records(area);
        if (!records.TryGetValue(key, out JObject? stored))
        {
            return Problem(HttpStatusCode.NotFound, "RecordWithNameNotFound");
        }

        if (version is not null && stored.Value<int>(VersionProperty) != version)
        {
            return Problem(HttpStatusCode.Conflict, "ConcurrencyConflict");
        }

        records.Remove(key);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private HttpResponseMessage WriteWithoutVersion(string area, string nameProperty, string key, JObject record)
    {
        StoreNextVersion(area, nameProperty, key, record);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private void StoreNextVersion(string area, string nameProperty, string key, JObject record)
    {
        int newVersion = VersionOf(area, key) + 1;
        record[nameProperty] = key;
        record[VersionProperty] = newVersion;
        Records(area).Remove(key);
        Records(area)[key] = record;
    }

    //merge=true only: adds or replaces every uploaded type; a gitignore type keeps the server's Id
    private HttpResponseMessage SyncUp(string area, string body, bool hasId)
    {
        foreach (JObject record in JArray.Parse(body).Cast<JObject>())
        {
            string name = record.Value<string>("Name")!;
            if (hasId)
            {
                record["Id"] = Records(area).TryGetValue(name, out JObject? stored)
                    ? stored["Id"]
                    : Guid.NewGuid().ToString();
            }

            StoreNextVersion(area, "Name", name, record);
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }

    private HttpResponseMessage DeleteWithoutVersion(string area, string key, string notFoundCode)
    {
        return Records(area).Remove(key)
            ? new HttpResponseMessage(HttpStatusCode.OK)
            : Problem(HttpStatusCode.NotFound, notFoundCode);
    }

    private static HttpResponseMessage Json(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    //CustomResults.Problem: the code is the title, ApiClient reads it as Error.Code
    private static HttpResponseMessage Problem(HttpStatusCode statusCode, string code)
    {
        string json = JsonConvert.SerializeObject(new { title = code, status = (int)statusCode, detail = code });
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/problem+json")
        };
    }
}
