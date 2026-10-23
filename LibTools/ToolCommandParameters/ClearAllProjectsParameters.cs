using System.Collections.Generic;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace LibTools.ToolCommandParameters;

public sealed class ClearAllProjectsParameters : IParameters
{
    private ClearAllProjectsParameters(Dictionary<string, ProjectModel> projects, string? projectGroupName,
        string? projectName)
    {
        Projects = projects;
        ProjectGroupName = projectGroupName;
        ProjectName = projectName;
    }

    public Dictionary<string, ProjectModel> Projects { get; }
    public string? ProjectGroupName { get; }
    public string? ProjectName { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ClearAllProjectsParameters Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName, string? projectName)
    {
        return new ClearAllProjectsParameters(supportToolsParameters.Projects, projectGroupName, projectName);
    }
}
