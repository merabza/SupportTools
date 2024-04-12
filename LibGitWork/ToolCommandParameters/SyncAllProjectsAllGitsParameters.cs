using LibParameters;
using SupportToolsData.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using System.Collections.Generic;

namespace LibGitWork.ToolCommandParameters;

public class SyncAllProjectsAllGitsParameters : IParameters
{
    public string? ScaffoldSeedersWorkFolder { get; }
    public Dictionary<string, ProjectModel> Projects { get; }

    private SyncAllProjectsAllGitsParameters(string? scaffoldSeedersWorkFolder,
        Dictionary<string, ProjectModel> projects)
    {
        ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
        Projects = projects;
    }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static SyncAllProjectsAllGitsParameters Create(ILogger logger, SupportToolsParameters supportToolsParameters)
    {

        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
        {
            StShared.WriteWarningLine("ScaffoldSeedersWorkFolder is not specified", true);
        }

        return new SyncAllProjectsAllGitsParameters(supportToolsParameters.ScaffoldSeedersWorkFolder,
            supportToolsParameters.Projects);

    }
}