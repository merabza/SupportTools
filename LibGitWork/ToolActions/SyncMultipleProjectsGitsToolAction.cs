using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork.ToolActions;

public class SyncMultipleProjectsGitsToolAction : ToolAction
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly SyncMultipleProjectsGitsParameters _syncMultipleProjectsGitsParameters;

    private SyncMultipleProjectsGitsToolAction(ILogger logger, ParametersManager parametersManager,
        SyncMultipleProjectsGitsParameters syncMultipleProjectsGitsParameters) : base(logger,
        "Sync One Group All Projects Gits",
        null, null)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _syncMultipleProjectsGitsParameters = syncMultipleProjectsGitsParameters;
    }


    public static SyncMultipleProjectsGitsToolAction Create(ILogger logger, ParametersManager parametersManager,
        string? projectGroupName, string? projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var syncMultipleProjectsGitsParameters =
            SyncMultipleProjectsGitsParameters.Create(supportToolsParameters, projectGroupName, projectName);

        return new SyncMultipleProjectsGitsToolAction(logger, parametersManager, syncMultipleProjectsGitsParameters);
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList;
        if (_syncMultipleProjectsGitsParameters.ProjectGroupName is null &&
            _syncMultipleProjectsGitsParameters.ProjectName is null)
            projectsList = _syncMultipleProjectsGitsParameters.Projects;
        else if (_syncMultipleProjectsGitsParameters.ProjectGroupName is not null)
            projectsList = _syncMultipleProjectsGitsParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _syncMultipleProjectsGitsParameters.ProjectGroupName);
        else
            projectsList =
                _syncMultipleProjectsGitsParameters.Projects.Where(x =>
                    x.Key == _syncMultipleProjectsGitsParameters.ProjectName);

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        var changedGitProjects = new Dictionary<EGitCollect, Dictionary<string, List<string>>>
        {
            [EGitCollect.Collect] = [],
            [EGitCollect.Usage] = []
        };
        var loopNom = 0;
        var gitCollectUsage = EGitCollect.Collect;
        while (gitCollectUsage == EGitCollect.Collect || changedGitProjects[EGitCollect.Collect].Count > 0)
        {
            changedGitProjects[EGitCollect.Collect] = [];
            Console.WriteLine($"---=== {gitCollectUsage} {(loopNom == 0 ? "" : loopNom)} ===---");
            //პროექტების ჩამონათვალი
            foreach (var kvp in projectsListOrdered)
            {
                SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.Main, changedGitProjects, loopNom == 0);
                if (_syncMultipleProjectsGitsParameters.ScaffoldSeedersWorkFolder is not null)
                    SyncAllGitsForOneProject(kvp.Key, kvp.Value, EGitCol.ScaffoldSeed, changedGitProjects,
                        loopNom == 0);
            }

            Console.WriteLine("---===---------===---");

            gitCollectUsage = EGitCollect.Usage;
            loopNom++;
            changedGitProjects[EGitCollect.Usage] = changedGitProjects[EGitCollect.Collect];
        }

        return Task.FromResult(true);
    }

    private void SyncAllGitsForOneProject(string projectName, ProjectModel project, EGitCol gitCol,
        Dictionary<EGitCollect, Dictionary<string, List<string>>> changedGitProjects, bool isFirstSync)
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

        var syncAllGitsCliMenuCommandMain = SyncOneProjectAllGitsToolAction.Create(_logger, _parametersManager,
            projectName, gitCol, changedGitProjects, isFirstSync);
        syncAllGitsCliMenuCommandMain?.Run(CancellationToken.None).Wait();
    }
}