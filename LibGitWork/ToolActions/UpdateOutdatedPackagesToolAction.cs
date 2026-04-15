using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;

namespace LibGitWork.ToolActions;

public sealed class UpdateOutdatedPackagesToolAction : ToolAction
{
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly UpdateOutdatedPackagesParameters _updateOutdatedPackagesParameters;

    private UpdateOutdatedPackagesToolAction(ILogger? logger, ParametersManager parametersManager,
        UpdateOutdatedPackagesParameters updateOutdatedPackagesParameters, bool useConsole) : base(logger,
        "Update Outdated Packages", null, null, useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _updateOutdatedPackagesParameters = updateOutdatedPackagesParameters;
    }

    public static UpdateOutdatedPackagesToolAction Create(ILogger logger, ParametersManager parametersManager,
        string? projectGroupName, string? projectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var updateOutdatedPackagesParameters =
            UpdateOutdatedPackagesParameters.Create(supportToolsParameters, projectGroupName, projectName);
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new UpdateOutdatedPackagesToolAction(loggerOrNull, parametersManager, updateOutdatedPackagesParameters,
            useConsole);
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var syncOneProjectAllGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, null, null, true);
        await syncOneProjectAllGitsToolAction.Run(cancellationToken);

        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList = GetProjectsList();

        List<KeyValuePair<string, ProjectModel>> projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        //var packageUpdaters = new Dictionary<string, PackageUpdater>();
        var projectGitProjectNames = new Dictionary<string, List<string>>();
        //var updatedGitProjectNames = new List<string>();
        const EGitCol gitCol = EGitCol.Main;

        foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        {
            projectGitProjectNames.Add(projectName, project.GetGitProjectNamesByGitCollectionType(gitCol).ToList());
        }

        int lastProjectsCount = projectGitProjectNames.Count + 1;
        while (projectGitProjectNames.Count > 0 && projectGitProjectNames.Count < lastProjectsCount)
        {
            lastProjectsCount = projectGitProjectNames.Count;
            foreach (KeyValuePair<string, List<string>> kvp in projectGitProjectNames.Where(x => x.Value.Count == 1)
                         .ToList())
            {
                string projectName = kvp.Key;

                var syncOneGroupAllProjectsGitsToolAction =
                    SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, null, projectName, true);
                await syncOneGroupAllProjectsGitsToolAction.Run(cancellationToken);

                string gitProjectName = kvp.Value[0];
                var packageUpdaterToolAction = PackageUpdaterToolAction.Create(_logger, _parametersManager, projectName,
                    gitCol, gitProjectName, true);
                packageUpdaterToolAction?.RunPackageUpdate();

                foreach (KeyValuePair<string, List<string>> pair in projectGitProjectNames)
                {
                    pair.Value.Remove(gitProjectName);
                }

                projectGitProjectNames.Remove(projectName);
                projectGitProjectNames.Where(x => x.Value.Count == 0).ToList()
                    .ForEach(x => projectGitProjectNames.Remove(x.Key));

                await syncOneGroupAllProjectsGitsToolAction.Run(cancellationToken);
            }
        }

        //foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        //{
        //    foreach (string gitProjectName in project.GetGitProjectNamesByGitCollectionType(gitCol))
        //    {
        //        if (!packageUpdaters.TryGetValue(gitProjectName, out PackageUpdater? value))
        //        {
        //            value = new PackageUpdater(_logger, _parametersManager, gitProjectName, true);
        //            packageUpdaters.Add(gitProjectName, value);
        //        }

        //        value.Add(projectName, gitCol);
        //    }
        //}

        //foreach (KeyValuePair<string, PackageUpdater> keyValuePair in packageUpdaters.Where(x => x.Value.Count > 0)
        //             .OrderBy(x => x.Key))
        //{
        //    PackageUpdater packageUpdater = keyValuePair.Value;
        //    packageUpdater.Run();
        //}

        return true;
    }

    private IEnumerable<KeyValuePair<string, ProjectModel>> GetProjectsList()
    {
        if (_updateOutdatedPackagesParameters.ProjectGroupName is null &&
            _updateOutdatedPackagesParameters.ProjectName is null)
        {
            return _updateOutdatedPackagesParameters.Projects;
        }

        if (_updateOutdatedPackagesParameters.ProjectGroupName is not null)
        {
            return _updateOutdatedPackagesParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _updateOutdatedPackagesParameters.ProjectGroupName);
        }

        return _updateOutdatedPackagesParameters.Projects.Where(x =>
            x.Key == _updateOutdatedPackagesParameters.ProjectName);
    }
}
