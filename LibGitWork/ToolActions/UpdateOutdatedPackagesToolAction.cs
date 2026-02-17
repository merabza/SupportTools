using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using ToolsManagement.LibToolActions;

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

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList = GetProjectsList();

        List<KeyValuePair<string, ProjectModel>> projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        var gitSyncToolsByGitProjectNames = new Dictionary<string, PackageUpdater>();
        const EGitCol gitCol = EGitCol.Main;

        foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        {
            foreach (string gitProjectName in project.GetGitProjectNamesByGitCollectionType(gitCol))
            {
                if (!gitSyncToolsByGitProjectNames.TryGetValue(gitProjectName, out PackageUpdater? value))
                {
                    value = new PackageUpdater(_logger, _parametersManager, gitProjectName, true);
                    gitSyncToolsByGitProjectNames.Add(gitProjectName, value);
                }

                value.Add(projectName, gitCol);
            }
        }

        foreach (KeyValuePair<string, PackageUpdater> keyValuePair in gitSyncToolsByGitProjectNames
                     .Where(x => x.Value.Count > 0).OrderBy(x => x.Key))
        {
            PackageUpdater packageUpdater = keyValuePair.Value;
            packageUpdater.Run();
        }

        return ValueTask.FromResult(true);
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
