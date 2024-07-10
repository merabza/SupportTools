using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace LibGitWork.ToolActions;

public class SyncMultipleProjectsGitsToolAction : ToolAction
{
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly SyncMultipleProjectsGitsParameters _syncMultipleProjectsGitsParameters;

    private SyncMultipleProjectsGitsToolAction(ILogger? logger, ParametersManager parametersManager,
        SyncMultipleProjectsGitsParameters syncMultipleProjectsGitsParameters, bool useConsole) : base(logger,
        "Sync Multiple Projects Gits", null, null, useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _syncMultipleProjectsGitsParameters = syncMultipleProjectsGitsParameters;
    }


    public static SyncMultipleProjectsGitsToolAction Create(ILogger logger, ParametersManager parametersManager,
        string? projectGroupName, string? projectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var syncMultipleProjectsGitsParameters =
            SyncMultipleProjectsGitsParameters.Create(supportToolsParameters, projectGroupName, projectName);
        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new SyncMultipleProjectsGitsToolAction(loggerOrNull, parametersManager,
            syncMultipleProjectsGitsParameters,
            useConsole);
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
            Console.WriteLine($"---=== {gitCollectUsage} {(loopNom == 0 ? string.Empty : loopNom)} ===---");
            //პროექტების ჩამონათვალი
            foreach (var (projectName, project) in projectsListOrdered)
            {
                SyncAllGitsForOneProject(projectName, project, EGitCol.Main, changedGitProjects, loopNom == 0);
                if (_syncMultipleProjectsGitsParameters.ScaffoldSeedersWorkFolder is not null)
                    SyncAllGitsForOneProject(projectName, project, EGitCol.ScaffoldSeed, changedGitProjects,
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
        if (!GitStat.CheckGitProject(projectName, project, gitCol))
            return;

        var syncAllGitsCliMenuCommandMain = SyncOneProjectAllGitsToolAction.Create(_logger, _parametersManager,
            projectName, gitCol, changedGitProjects, isFirstSync, UseConsole);
        syncAllGitsCliMenuCommandMain?.Run(CancellationToken.None).Wait();
    }
}