using System;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.Models;

public sealed class JsonFromProjectDbProjectGetterParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public JsonFromProjectDbProjectGetterParameters(string getJsonFromScaffoldDbProjectFileFullName,
        string getJsonFromScaffoldDbProjectParametersFileFullName)
    {
        GetJsonFromScaffoldDbProjectFileFullName = getJsonFromScaffoldDbProjectFileFullName;
        GetJsonFromScaffoldDbProjectParametersFileFullName = getJsonFromScaffoldDbProjectParametersFileFullName;
    }

    public string GetJsonFromScaffoldDbProjectFileFullName { get; set; }
    public string GetJsonFromScaffoldDbProjectParametersFileFullName { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static JsonFromProjectDbProjectGetterParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        try
        {
            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (string.IsNullOrWhiteSpace(project.GetJsonFromScaffoldDbProjectFileFullName))
            {
                StShared.WriteErrorLine(
                    $"GetJsonFromScaffoldDbProjectFileFullName does not specified for project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.GetJsonFromScaffoldDbProjectParametersFileFullName))
            {
                StShared.WriteErrorLine(
                    $"GetJsonFromScaffoldDbProjectParametersFileFullName does not specified for project {projectName}",
                    true);
                return null;
            }

            var jsonFromProjectDbProjectGetterParameters = new JsonFromProjectDbProjectGetterParameters(
                project.GetJsonFromScaffoldDbProjectFileFullName,
                project.GetJsonFromScaffoldDbProjectParametersFileFullName);
            return jsonFromProjectDbProjectGetterParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}