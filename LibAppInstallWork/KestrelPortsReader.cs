using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork;

public static partial class KestrelPortsReader
{
    //appsettings-ის Kestrel:Endpoints:<სახელი>:Url-ების პორტები, მაგალითად http://*:5051 → 5051
    //პორტის გარეშე მისამართი გამოტოვებულია
    public static List<int> Read(string appSettingsJson)
    {
        JObject appSettings = JObject.Parse(appSettingsJson);
        if (appSettings["Kestrel"]?["Endpoints"] is not JObject endpoints)
        {
            return [];
        }

        return
        [
            .. endpoints.Properties().Select(p => p.Value is JObject endpoint ? endpoint["Url"] : null)
                .Select(url => url?.Type == JTokenType.String ? GetPort(url.Value<string>()) : null).OfType<int>()
                .Distinct()
        ];
    }

    //პორტები appsettings ფაილიდან. ფაილი თუ არ არის მითითებული, არ არსებობს ან json არ არის, აბრუნებს null-ს და
    //error-ში მიზეზს
    public static List<int>? ReadFile(string? appSettingsFileName, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(appSettingsFileName))
        {
            error = "App Settings Json Source File Name is not specified";
            return null;
        }

        if (!File.Exists(appSettingsFileName))
        {
            error = $"App Settings Json Source File {appSettingsFileName} does not exist";
            return null;
        }

        try
        {
            return Read(File.ReadAllText(appSettingsFileName));
        }
        catch (JsonReaderException ex)
        {
            error = $"App Settings Json Source File {appSettingsFileName} is not valid json: {ex.Message}";
        }
        catch (IOException ex)
        {
            error = $"App Settings Json Source File {appSettingsFileName} can not be read: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            error = $"App Settings Json Source File {appSettingsFileName} can not be read: {ex.Message}";
        }

        return null;
    }

    //გასახსნელი პორტები: OpenPort-ის გარეშე ცარიელი სია. შეცდომას (ფაილი არ იკითხება, პორტი არ არის) ბეჭდავს და
    //აბრუნებს null-ს
    public static List<int>? GetPortsToOpen(ServerInfoModel serverInfo, string projectName)
    {
        if (!serverInfo.OpenPort)
        {
            return [];
        }

        List<int>? ports = ReadFile(serverInfo.AppSettingsJsonSourceFileName, out string? error);
        if (ports is null)
        {
            StShared.WriteErrorLine(
                $"Open Port is on for project {projectName} and server {serverInfo.GetItemKey()}, but {error}", true);
            return null;
        }

        if (ports.Count != 0)
        {
            return ports;
        }

        StShared.WriteErrorLine(
            $"Open Port is on for project {projectName} and server {serverInfo.GetItemKey()}, but {serverInfo.AppSettingsJsonSourceFileName} has no Kestrel endpoint port",
            true);
        return null;
    }

    private static int? GetPort(string? url)
    {
        if (url is null)
        {
            return null;
        }

        Match match = UrlPortRegex().Match(url.Trim());
        return match.Success &&
               int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int port) &&
               port is > 0 and <= 65535
            ? port
            : null;
    }

    //scheme://host:port[/path], host შეიძლება იყოს *, + ან [IPv6]
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9+.-]*://(?:\[[^\]]*\]|[^/:\[\]]*):([0-9]{1,5})(?:/.*)?\z")]
    private static partial Regex UrlPortRegex();
}
