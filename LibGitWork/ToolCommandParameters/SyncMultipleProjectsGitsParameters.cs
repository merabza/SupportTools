using System.Collections.Generic;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork.ToolCommandParameters;

public class SyncMultipleProjectsGitsParameters : IParameters
{
    private SyncMultipleProjectsGitsParameters(string? scaffoldSeedersWorkFolder,
        Dictionary<string, ProjectModel> projects, string? projectGroupName)
    {
        ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
        Projects = projects;
        ProjectGroupName = projectGroupName;
    }

    public string? ScaffoldSeedersWorkFolder { get; }
    public Dictionary<string, ProjectModel> Projects { get; }
    public string? ProjectGroupName { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static SyncMultipleProjectsGitsParameters Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName)
    {
        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
            StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);

        return new SyncMultipleProjectsGitsParameters(supportToolsParameters.ScaffoldSeedersWorkFolder,
            supportToolsParameters.Projects, projectGroupName);
    }
}