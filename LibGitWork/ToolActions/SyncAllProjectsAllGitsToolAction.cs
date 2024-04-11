using System.Linq;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SupportToolsData;
using SystemToolsShared;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace LibGitWork.ToolActions;

public class SyncAllProjectsAllGitsToolAction : ToolAction
{
    private readonly SyncAllProjectsAllGitsParameters _syncAllProjectsAllGitsParameters;

    private SyncAllProjectsAllGitsToolAction(ILogger logger,
        SyncAllProjectsAllGitsParameters syncAllProjectsAllGitsParameters) : base(logger, "Sync All Projects All Gits",
        null, null)
    {
        _syncAllProjectsAllGitsParameters = syncAllProjectsAllGitsParameters;
    }


    public static SyncAllProjectsAllGitsToolAction Create(ILogger logger, ParametersManager parametersManager)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var syncAllProjectsAllGitsParameters = SyncAllProjectsAllGitsParameters.Create(logger, supportToolsParameters);

        return new SyncAllProjectsAllGitsToolAction(logger, syncAllProjectsAllGitsParameters);
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        foreach (var kvp in _syncAllProjectsAllGitsParameters.Projects.OrderBy(o => o.Key))
        {
            SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.Main);
            if (_syncAllProjectsAllGitsParameters.ScaffoldSeedersWorkFolder is not null)
                SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.ScaffoldSeed);
        }

        return Task.FromResult(true);
    }
    private static void SyncAllGitsForOneProject(string projectName, ProjectModel project, EGitCol gitCol)
    {
        switch (gitCol)
        {
            case EGitCol.Main:
                if (!string.IsNullOrWhiteSpace(project.ProjectFolderName))
                    break;
                StShared.WriteErrorLine($"ProjectFolderName is not specified for project {projectName}", true);
                return;
            case EGitCol.ScaffoldSeed:
                if (!string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
                    break;
                StShared.WriteWarningLine($"ScaffoldSeederProjectName is not specified for project {projectName}",
                    true);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(gitCol), gitCol, null);
        }
    }
}