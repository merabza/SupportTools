using System.Collections.Generic;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace LibGitWork.ToolCommandParameters;

public sealed class SyncMultipleProjectsGitsParameters : IParameters
{
    private SyncMultipleProjectsGitsParameters(Dictionary<string, ProjectModel> projects, string? projectGroupName,
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

    public static SyncMultipleProjectsGitsParameters Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName, string? projectName)
    {
        return new SyncMultipleProjectsGitsParameters(supportToolsParameters.Projects, projectGroupName, projectName);
    }
}
