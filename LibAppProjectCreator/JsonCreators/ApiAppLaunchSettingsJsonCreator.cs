using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.JsonCreators;

public sealed class ApiAppLaunchSettingsJsonCreator
{
    private readonly string _projectFullPath;
    private readonly string _projectName;
    private readonly bool _useReact;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApiAppLaunchSettingsJsonCreator(bool useReact, string projectName, string projectFullPath)
    {
        _useReact = useReact;
        _projectName = projectName;
        _projectFullPath = projectFullPath;
    }


    public bool Create()
    {
        var projObject = new JObject(new JProperty("commandName", "Project"),
            new JProperty("workingDirectory", _projectFullPath),
            new JProperty("launchBrowser", true), new JProperty("externalUrlConfiguration", true));
        if (!_useReact)
            projObject.Add(new JProperty("launchUrl", "swagger"));
        projObject.Add(new JProperty("environmentVariables",
            new JObject(new JProperty("ASPNETCORE_ENVIRONMENT", "Development"))));
        projObject.Add(new JProperty("applicationUrl", "http://localhost:5099"));

        var launchSettingsJObject = new JObject(
            new JProperty("profiles", new JObject(
                new JProperty(_projectName, projObject))));
        var launchSettingsJsonFileName =
            Path.Combine(_projectFullPath, "Properties", "launchSettings.json");
        File.WriteAllText(launchSettingsJsonFileName, launchSettingsJObject.ToString(Formatting.Indented));

        return true;
    }
}