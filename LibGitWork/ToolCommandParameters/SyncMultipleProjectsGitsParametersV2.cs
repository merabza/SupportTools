using System.Collections.Generic;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolCommandParameters;

public sealed class SyncMultipleProjectsGitsParametersV2 : IParameters
{
    private SyncMultipleProjectsGitsParametersV2(string? scaffoldSeedersWorkFolder,
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

    public static SyncMultipleProjectsGitsParametersV2 Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName, string? projectName)
    {
        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
        {
            StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);
        }

        return new SyncMultipleProjectsGitsParametersV2(supportToolsParameters.ScaffoldSeedersWorkFolder,
            supportToolsParameters.Projects, projectGroupName, projectName);
    }
}
