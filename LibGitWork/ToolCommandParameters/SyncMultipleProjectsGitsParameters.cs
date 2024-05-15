using System.Collections.Generic;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork.ToolCommandParameters;

public class SyncMultipleProjectsGitsParameters : IParameters
{
    private SyncMultipleProjectsGitsParameters(string? scaffoldSeedersWorkFolder,
        Dictionary<string, ProjectModel> projects, string? projectGroupName, string? projectName)
    {
        ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
        Projects = projects;
        ProjectGroupName = projectGroupName;
        ProjectName = projectName;
    }

    public string? ScaffoldSeedersWorkFolder { get; }
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
        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
            StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);

        return new SyncMultipleProjectsGitsParameters(supportToolsParameters.ScaffoldSeedersWorkFolder,
            supportToolsParameters.Projects, projectGroupName, projectName);
    }
}