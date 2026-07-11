using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibDotnetWork;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.BaGetter;

//BaGetter სერვერთან სამუშაო კლიენტი nuget V3 API-ის საშუალებით.
//არსებული ApiClient საბაზისო კლასი api key-ს query string-ში სვამს,
//nuget-ის წაშლის API-ს კი key სჭირდება X-NuGet-ApiKey ჰედერში, ამიტომ ეს კლასი დამოუკიდებელია
public sealed class BaGetterApiClient
{
    private const string ApiKeyHeaderName = "X-NuGet-ApiKey";

    private readonly string? _apiKey;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly string _server;

    //service index-იდან ერთხელ წაკითხული რესურსების მისამართები ინახება ქეშში
    private string? _packagePublishAddress;
    private string? _searchQueryServiceAddress;

    // ReSharper disable once ConvertToPrimaryConstructor
    public BaGetterApiClient(ILogger logger, IHttpClientFactory httpClientFactory, string server, string? apiKey)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _server = server;
        _apiKey = apiKey;
    }

    //სერვერზე არსებული ყველა პაკეტის სახელის მოძიება search სერვისის საშუალებით.
    //search გამოიყენება იმიტომ, რომ ის მხოლოდ ხილულ (listed) ვერსიებს აბრუნებს და წაშლილები აღარ ჩანს
    public async Task<List<string>?> GetPackageIds(CancellationToken cancellationToken = default)
    {
        try
        {
            JObject? searchResult = await GetSearchResult(string.Empty, cancellationToken);
            if (searchResult is null)
            {
                return null;
            }

            List<string> packageIds = searchResult["data"]?.Select(x => (string?)x["id"]).OfType<string>()
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList() ?? [];

            int totalHits = (int?)searchResult["totalHits"] ?? 0;
            if (totalHits > packageIds.Count)
            {
                StShared.WriteWarningLine(
                    $"Server contains {totalHits} packages, but only {packageIds.Count} was returned", true, _logger);
            }

            return packageIds;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true, _logger);
            return null;
        }
    }

    //ერთი პაკეტის ხილული (listed) ვერსიების სიის მოძიება
    public async Task<List<string>?> GetVersions(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            JObject? searchResult = await GetSearchResult(Uri.EscapeDataString(packageId), cancellationToken);
            if (searchResult is null)
            {
                return null;
            }

            //search პაკეტებს სახელის ნაწილითაც პოულობს, ამიტომ საჭიროა ზუსტი დამთხვევის ამორჩევა
            JToken? packageToken = searchResult["data"]?.FirstOrDefault(x =>
                string.Equals((string?)x["id"], packageId, StringComparison.OrdinalIgnoreCase));

            if (packageToken is not null)
            {
                return packageToken["versions"]?.Select(x => (string?)x["version"]).OfType<string>().ToList() ?? [];
            }

            StShared.WriteErrorLine($"Package {packageId} does not found on server", true, _logger);
            return null;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true, _logger);
            return null;
        }
    }

    //პაკეტის ერთი ვერსიის წაშლა სერვერიდან.
    //რეალური ქცევა სერვერის PackageDeletionBehavior პარამეტრზეა დამოკიდებული (Unlist ან HardDelete)
    public async Task<bool> DeleteVersion(string packageId, string version,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await LoadServiceIndex(cancellationToken))
            {
                return false;
            }

            var requestUri = new Uri(
                $"{_packagePublishAddress}/{packageId.ToLowerInvariant()}/{version.ToLowerInvariant()}");

            // ReSharper disable once using
            // ReSharper disable once DisposableConstructor
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Add(ApiKeyHeaderName, _apiKey);
            }

            // ReSharper disable once using
            using HttpClient client = _httpClientFactory.CreateClient();
            // ReSharper disable once using
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound:
                    StShared.WriteErrorLine($"Package {packageId} version {version} does not found on server", true,
                        _logger);
                    return false;
                case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                    StShared.WriteErrorLine(
                        $"Cannot delete package {packageId} version {version}. Api key does not specified or is invalid",
                        true, _logger);
                    return false;
            }

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            StShared.WriteErrorLine(
                $"Cannot delete package {packageId} version {version}. Status code is {response.StatusCode}", true,
                _logger);
            return false;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true, _logger);
            return false;
        }
    }

    //search სერვისის გამოძახება და პასუხის JSON-ის წაკითხვა
    private async Task<JObject?> GetSearchResult(string query, CancellationToken cancellationToken)
    {
        if (!await LoadServiceIndex(cancellationToken))
        {
            return null;
        }

        var uri = new Uri($"{_searchQueryServiceAddress}?q={query}&skip=0&take=1000&prerelease=true&semVerLevel=2.0.0");

        // ReSharper disable once using
        using HttpClient client = _httpClientFactory.CreateClient();
        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(uri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            StShared.WriteErrorLine($"Cannot get search result from {uri}. Status code is {response.StatusCode}", true,
                _logger);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JObject.Parse(content);
    }

    //საჭირო რესურსების მისამართების დადგენა სერვერის service index-იდან.
    //სხვადასხვა სერვერს ეს მისამართები სხვადასხვა აქვს, ამიტომ მათი აღმოჩენა ხდება რესურსის ტიპით
    private async Task<bool> LoadServiceIndex(CancellationToken cancellationToken)
    {
        if (_searchQueryServiceAddress is not null && _packagePublishAddress is not null)
        {
            return true;
        }

        var serviceIndexUri = new Uri(NugetServerUrls.GetServiceIndexUrl(_server));

        // ReSharper disable once using
        using HttpClient client = _httpClientFactory.CreateClient();
        // ReSharper disable once using
        using HttpResponseMessage response = await client.GetAsync(serviceIndexUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            StShared.WriteErrorLine(
                $"Cannot get nuget service index {serviceIndexUri}. Status code is {response.StatusCode}", true,
                _logger);
            return false;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        JToken? resources = JObject.Parse(content)["resources"];
        string? searchQueryServiceAddress =
            resources?.FirstOrDefault(x => (string?)x["@type"] == "SearchQueryService")?["@id"]?.ToString();
        string? packagePublishAddress =
            resources?.FirstOrDefault(x => (string?)x["@type"] == "PackagePublish/2.0.0")?["@id"]?.ToString();

        if (string.IsNullOrWhiteSpace(searchQueryServiceAddress))
        {
            StShared.WriteErrorLine(
                $"SearchQueryService resource does not found in nuget service index {serviceIndexUri}", true, _logger);
            return false;
        }

        if (string.IsNullOrWhiteSpace(packagePublishAddress))
        {
            StShared.WriteErrorLine(
                $"PackagePublish/2.0.0 resource does not found in nuget service index {serviceIndexUri}", true,
                _logger);
            return false;
        }

        _searchQueryServiceAddress = searchQueryServiceAddress.TrimEnd('/');
        _packagePublishAddress = packagePublishAddress.TrimEnd('/');
        return true;
    }
}
