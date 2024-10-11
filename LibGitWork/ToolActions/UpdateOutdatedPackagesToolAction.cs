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

public class UpdateOutdatedPackagesToolAction : ToolAction
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
        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new UpdateOutdatedPackagesToolAction(loggerOrNull, parametersManager, updateOutdatedPackagesParameters,
            useConsole);
    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList;
        if (_updateOutdatedPackagesParameters.ProjectGroupName is null &&
            _updateOutdatedPackagesParameters.ProjectName is null)
            projectsList = _updateOutdatedPackagesParameters.Projects;
        else if (_updateOutdatedPackagesParameters.ProjectGroupName is not null)
            projectsList = _updateOutdatedPackagesParameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _updateOutdatedPackagesParameters.ProjectGroupName);
        else
            projectsList =
                _updateOutdatedPackagesParameters.Projects.Where(x =>
                    x.Key == _updateOutdatedPackagesParameters.ProjectName);

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        var gitSyncToolsByGitProjectNames = new Dictionary<string, PackageUpdater>();
        const EGitCol gitCol = EGitCol.Main;


        foreach (var (projectName, project) in projectsListOrdered)
        {
            foreach (var gitProjectName in project.GetGitProjectNames(gitCol))
            {
                if (!gitSyncToolsByGitProjectNames.ContainsKey(gitProjectName))
                    gitSyncToolsByGitProjectNames.Add(gitProjectName,
                        new PackageUpdater(_logger, _parametersManager, gitProjectName, true));
                gitSyncToolsByGitProjectNames[gitProjectName].Add(projectName, gitCol);
            }
        }

        foreach (var keyValuePair in gitSyncToolsByGitProjectNames.Where(x => x.Value.Count > 0).OrderBy(x => x.Key))
        {
            var packageUpdater = keyValuePair.Value;
            packageUpdater.Run();
        }

        return Task.FromResult(true);
    }
}