using LibParameters;
using SupportToolsData.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork.ToolCommandParameters;

public class SyncOneGroupAllProjectsGitsParameters : IParameters
{
    public string? ScaffoldSeedersWorkFolder { get; }
    public Dictionary<string, ProjectModel> Projects { get; }
    public string ProjectGroupName { get; }

    private SyncOneGroupAllProjectsGitsParameters(string? scaffoldSeedersWorkFolder,
        Dictionary<string, ProjectModel> projects, string projectGroupName)
    {
        ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
        Projects = projects;
        ProjectGroupName = projectGroupName;
    }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static SyncOneGroupAllProjectsGitsParameters Create(ILogger logger, SupportToolsParameters supportToolsParameters, string projectGroupName)
    {

        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
        {
            StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);
        }

        return new SyncOneGroupAllProjectsGitsParameters(supportToolsParameters.ScaffoldSeedersWorkFolder,
            supportToolsParameters.Projects, projectGroupName);

    }


}