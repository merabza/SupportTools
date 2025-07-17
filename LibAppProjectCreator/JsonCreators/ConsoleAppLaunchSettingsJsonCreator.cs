using System.IO;
using Newtonsoft.Json.Linq;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.JsonCreators;

public sealed class ConsoleAppLaunchSettingsJsonCreator
{
    private readonly string _placePath;
    private readonly string _projectName;
    private readonly string _projectParametersFilePath;

    public ConsoleAppLaunchSettingsJsonCreator(string placePath, string projectName, string projectParametersFilePath)
    {
        _placePath = placePath;
        _projectName = projectName;
        _projectParametersFilePath = projectParametersFilePath;
    }

    public bool Create()
    {
        var projectParametersFileFullName = Path.Combine(_projectParametersFilePath, $"{_projectName}.json");

        // ReSharper disable once CollectionNeverUpdated.Local
        var jObject = new JObject(new JProperty("profiles",
            new JObject(new JProperty(_projectName,
                new JObject(new JProperty("commandName", "Project"),
                    new JProperty("commandLineArgs", $"--use \"{projectParametersFileFullName}\""))))));

        var forCreateFileName = Path.Combine(_placePath, "launchSettings.json");
        File.WriteAllText(forCreateFileName, jObject.ToString());

        return true;
    }
}