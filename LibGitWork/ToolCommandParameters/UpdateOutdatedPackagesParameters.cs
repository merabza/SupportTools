using System.Collections.Generic;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace LibGitWork.ToolCommandParameters;

public sealed class UpdateOutdatedPackagesParameters : IParameters
{
    private UpdateOutdatedPackagesParameters(Dictionary<string, ProjectModel> projects, string? projectGroupName,
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

    public static UpdateOutdatedPackagesParameters Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName, string? projectName)
    {
        return new UpdateOutdatedPackagesParameters(supportToolsParameters.Projects, projectGroupName, projectName);
    }
}
