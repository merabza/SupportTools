using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolActions;

public sealed class UpdateOutdatedPackagesToolAction : ToolAction
{
    //false: მხოლოდ ამ კომპიუტერზე არსებული პროექტები მუშავდება და დანარჩენები არ იკლონება (D3)
    private readonly bool _includeRemoteProjects;

    //commit-ის შეტყობინების შეყვანა (ველის სახელი, ნაგულისხმევი მნიშვნელობა). ბრძანება კონსოლს აწვდის, ტესტები - თავიანთ პასუხს
    private readonly Func<string, string, string> _inputMessage;
    private readonly ILogger? _logger;
    private readonly IParametersManager _parametersManager;
    private readonly UpdateOutdatedPackagesParameters _updateOutdatedPackagesParameters;

    private UpdateOutdatedPackagesToolAction(ILogger? logger, IParametersManager parametersManager,
        UpdateOutdatedPackagesParameters updateOutdatedPackagesParameters, bool includeRemoteProjects, bool useConsole,
        Func<string, string, string> inputMessage) : base(logger, "Update Outdated Packages", null, null, useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _updateOutdatedPackagesParameters = updateOutdatedPackagesParameters;
        _includeRemoteProjects = includeRemoteProjects;
        _inputMessage = inputMessage;
    }

    public static UpdateOutdatedPackagesToolAction Create(ILogger logger, IParametersManager parametersManager,
        string? projectGroupName, string? projectName, bool includeRemoteProjects, bool useConsole,
        Func<string, string, string> inputMessage)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var updateOutdatedPackagesParameters =
            UpdateOutdatedPackagesParameters.Create(supportToolsParameters, projectGroupName, projectName);
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new UpdateOutdatedPackagesToolAction(loggerOrNull, parametersManager, updateOutdatedPackagesParameters,
            includeRemoteProjects, useConsole, inputMessage);
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        string useMessage = _inputMessage("Message", $"Update Packages {DateTime.Now.ToString("yyyyMMddHHmm",
            CultureInfo.InvariantCulture)}");

        var syncOneProjectAllGitsToolAction = SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager,
            null, null, true, $"{useMessage} Check", _includeRemoteProjects);
        await syncOneProjectAllGitsToolAction.Run(cancellationToken);

        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList =
            ProjectPresence.Filter(GetProjectsList(), _includeRemoteProjects);

        List<KeyValuePair<string, ProjectModel>> projectsListOrdered = [.. projectsList.OrderBy(o => o.Key)];

        //var packageUpdaters = new Dictionary<string, PackageUpdater>();
        var projectGitProjectNames = new Dictionary<string, List<string>>();
        //var updatedGitProjectNames = new List<string>();
        var failedGitProjectNames = new List<string>();

        foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        {
            projectGitProjectNames.Add(projectName, [.. project.GitProjectNames]);
        }

        int lastProjectsCount = projectGitProjectNames.Count + 1;
        while (projectGitProjectNames.Count > 0 && projectGitProjectNames.Count < lastProjectsCount)
        {
            lastProjectsCount = projectGitProjectNames.Count;
            foreach (KeyValuePair<string, List<string>> kvp in projectGitProjectNames.Where(x => x.Value.Count == 1)
                         .ToList())
            {
                string projectName = kvp.Key;

                var syncOneGroupAllProjectsGitsToolAction = SyncMultipleProjectsGitsToolActionV2.Create(_logger,
                    _parametersManager, null, projectName, true, $"{useMessage} Prepare");
                await syncOneGroupAllProjectsGitsToolAction.Run(cancellationToken);

                if (kvp.Value.Count == 0)
                {
                    continue;
                }

                string gitProjectName = kvp.Value[0];

                var packageUpdaterToolAction = PackageUpdaterToolAction.Create(_logger, _parametersManager, projectName,
                    gitProjectName, true);
                if (packageUpdaterToolAction is null || !packageUpdaterToolAction.RunPackageUpdate())
                {
                    failedGitProjectNames.Add(gitProjectName);
                }

                foreach (KeyValuePair<string, List<string>> pair in projectGitProjectNames)
                {
                    pair.Value.Remove(gitProjectName);
                }

                projectGitProjectNames.Remove(projectName);
                projectGitProjectNames.Where(x => x.Value.Count == 0).ToList()
                    .ForEach(x => projectGitProjectNames.Remove(x.Key));

                var syncOneGroupAllProjectsGitsToolAction2 = SyncMultipleProjectsGitsToolActionV2.Create(_logger,
                    _parametersManager, null, projectName, true, $"{useMessage} Update");
                await syncOneGroupAllProjectsGitsToolAction2.Run(cancellationToken);
            }
        }

        //foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        //{
        //    foreach (string gitProjectName in project.GitProjectNames)
        //    {
        //        if (!packageUpdaters.TryGetValue(gitProjectName, out PackageUpdater? value))
        //        {
        //            value = new PackageUpdater(_logger, _parametersManager, gitProjectName, true);
        //            packageUpdaters.Add(gitProjectName, value);
        //        }

        //        value.Add(projectName);
        //    }
        //}

        //foreach (KeyValuePair<string, PackageUpdater> keyValuePair in packageUpdaters.Where(x => x.Value.Count > 0)
        //             .OrderBy(x => x.Key))
        //{
        //    PackageUpdater packageUpdater = keyValuePair.Value;
        //    packageUpdater.Run();
        //}

        if (failedGitProjectNames.Count == 0)
        {
            return true;
        }

        //ჩავარდნილი რეპოების შეჯამება ბოლოს, ერთი პაუზით
        StShared.WriteErrorLine($"dotnet outdated failed for: {string.Join(", ", failedGitProjectNames)}", true,
            _logger);
        return false;
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
