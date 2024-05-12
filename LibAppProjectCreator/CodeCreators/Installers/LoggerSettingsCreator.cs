//Created by CreatorClassCreator at 7/2/2022 8:50:49 PM

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.CodeCreators.Installers;

public sealed class LoggerSettingsCreator
{
    private readonly JObject _appSettingsJsonJObject;
    private readonly List<string> _forEncodeAppSettingsJsonKeys;
    private readonly string _projectNamespace;
    private readonly JObject _userSecretJsonJObject;

    // ReSharper disable once ConvertToPrimaryConstructor
    public LoggerSettingsCreator(string projectNamespace, JObject appSettingsJsonJObject, JObject userSecretJsonJObject,
        List<string> forEncodeAppSettingsJsonKeys)
    {
        _projectNamespace = projectNamespace;
        _appSettingsJsonJObject = appSettingsJsonJObject;
        _userSecretJsonJObject = userSecretJsonJObject;
        _forEncodeAppSettingsJsonKeys = forEncodeAppSettingsJsonKeys;
    }

    public void Run()
    {
        _appSettingsJsonJObject.Add(new JProperty("Logging", new JObject(
            new JProperty("File", new JObject(
                new JProperty("LogLevel", new JObject(
                    new JProperty("Default", "Information"),
                    new JProperty("Microsoft", "Warning"),
                    new JProperty("Microsoft.Hosting.Lifetime", "Information"))))),
            new JProperty("Console", new JObject(
                new JProperty("IncludeScopes", true)))
        )));


        _appSettingsJsonJObject.Add(new JProperty("Serilog", new JObject(
            new JProperty("WriteTo", new JArray(
                new JObject(new JProperty("Name", "Console")),
                new JObject(new JProperty("Name", "File"),
                    new JProperty("Args", new JObject(
                        new JProperty("path", "PathToLogFile"),
                        new JProperty("rollingInterval", "Day")))))))));

        _userSecretJsonJObject.Add(new JProperty("Serilog", new JObject(
            new JProperty("WriteTo", new JArray(
                new JObject(new JProperty("Name", "Console")),
                new JObject(new JProperty("Name", "File"),
                    new JProperty("Args", new JObject(
                        new JProperty("path", $"D:\\Logs\\{_projectNamespace}\\{_projectNamespace}-logs.txt"),
                        new JProperty("rollingInterval", "Day")))))))));

        _forEncodeAppSettingsJsonKeys.Add("Serilog:WriteTo:1:Args:path");
    }
}