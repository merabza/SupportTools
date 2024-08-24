using System.IO;
using Newtonsoft.Json.Linq;

namespace LibAppProjectCreator.JsonCreators;

public sealed class ProjectParametersJsonCreator
{
    private readonly string _placePath;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectParametersJsonCreator(string placePath, string projectName)
    {
        _placePath = placePath;
        _projectName = projectName;
    }


    public bool Create()
    {
        var jObject = new JObject(new JProperty("LogFileName", $@"D:\Logs\{_projectName}\{_projectName}-log.txt"));
        var forCreateFileName = Path.Combine(_placePath, $"{_projectName}.json");
        File.WriteAllText(forCreateFileName, jObject.ToString());
        return true;
    }
}