using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using SupportToolsData.Models;
using SupportToolsData;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork.ToolActions;

public class SyncMultipleProjectsGitsToolAction : ToolAction
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly SyncMultipleProjectsGitsParameters _syncMultipleProjectsGitsParameters;

    private SyncMultipleProjectsGitsToolAction(ILogger logger, ParametersManager parametersManager,
        SyncMultipleProjectsGitsParameters syncMultipleProjectsGitsParameters) : base(logger, "Sync One Group All Projects Gits",
        null, null)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _syncMultipleProjectsGitsParameters = syncMultipleProjectsGitsParameters;
    }


    public static SyncMultipleProjectsGitsToolAction Create(ILogger logger, ParametersManager parametersManager, string? projectGroupName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var syncMultipleProjectsGitsParameters = SyncMultipleProjectsGitsParameters.Create(supportToolsParameters, projectGroupName);

        return new SyncMultipleProjectsGitsToolAction(logger, parametersManager, syncMultipleProjectsGitsParameters);
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var projectsList= _syncMultipleProjectsGitsParameters.ProjectGroupName is null
            ? _syncMultipleProjectsGitsParameters.Projects
            : _syncMultipleProjectsGitsParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _syncMultipleProjectsGitsParameters.ProjectGroupName);

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        var changedGitProjects = new List<string>();
        foreach (var gitCollect in Enum.GetValues<EGitCollect>())
        {
            //პროექტების ჩამონათვალი
            foreach (var kvp in projectsListOrdered)
            {
                SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.Main, changedGitProjects, gitCollect);
                if (_syncMultipleProjectsGitsParameters.ScaffoldSeedersWorkFolder is not null)
                    SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.ScaffoldSeed, changedGitProjects, gitCollect);
            }
        }

        return Task.FromResult(true);
    }
    private void SyncAllGitsForOneProject(string projectName, ProjectModel project, EGitCol gitCol, List<string> changedGitProjects, EGitCollect gitCollect)
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

        var syncAllGitsCliMenuCommandMain = SyncOneProjectAllGitsToolAction.Create(_logger, _parametersManager, projectName, gitCol, changedGitProjects, gitCollect);
        syncAllGitsCliMenuCommandMain?.Run(CancellationToken.None).Wait();
    }
}