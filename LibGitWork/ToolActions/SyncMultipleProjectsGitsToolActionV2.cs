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

public class SyncMultipleProjectsGitsToolActionV2 : ToolAction
{
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly SyncMultipleProjectsGitsParametersV2 _syncMultipleProjectsGitsParametersV2;

    private SyncMultipleProjectsGitsToolActionV2(ILogger? logger, ParametersManager parametersManager,
        SyncMultipleProjectsGitsParametersV2 syncMultipleProjectsGitsParametersV2, bool useConsole) : base(logger,
        "Sync Multiple Projects Gits V2", null, null, useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _syncMultipleProjectsGitsParametersV2 = syncMultipleProjectsGitsParametersV2;
    }


    public static SyncMultipleProjectsGitsToolActionV2 Create(ILogger logger, ParametersManager parametersManager,
        string? projectGroupName, string? projectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var syncMultipleProjectsGitsParametersV2 =
            SyncMultipleProjectsGitsParametersV2.Create(supportToolsParameters, projectGroupName, projectName);
        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new SyncMultipleProjectsGitsToolActionV2(loggerOrNull, parametersManager,
            syncMultipleProjectsGitsParametersV2,
            useConsole);
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        //პირველი ვერსიისგან განსხვავებით აქ აქცენტი უნდა გავაკეთოთ გიტის რეპოზიტორიების მიხედვით დაჯგუფებაზე.
        //ეს საშუალებას მოგვცემს რომ თუ დასასინქრონიზებელი იქნება ერთიდაიგივე მისამართის გიტი სხვადასხვა ფოლდერიდან,
        //არ მივმართოთ გიტის სერვერს რამდენჯერმე გიტის მხარეს არსებული ვერსიის იდენტიფიკატორის დასადგენად

        //1. უნდა დავადგინოთ ყველა ფოლდერი, რომელსაც სჭირდება სინქრონიზაცია
        //2. თითოეული ამ ფოლდერისათვის დავადგინოთ გიტის მისამართი
        //3. მიღებული სია დავაჯგუფოთ გიტის მისამართების მიხედვით
        //4. თითოეული გიტის მისამართისთვის,
        //  4.1. გავიაროთ სინქრონიზაცია ყველა ფოლდერისთვის, რომელიც მის ჯგუფში აღმოჩნდა,
        //  ოღონდ სერვერის მხარეს ინფორმაციის წამოღება უნდა მოხდეს ერთხელ თავიდან
        //  და თუ რამე დაიფუშა, ყოველი დაფუშვის მერე, ოღონდ თუ დარჩენილია დასასინქრონიზებელი ფოლდერი


        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList;
        if (_syncMultipleProjectsGitsParametersV2.ProjectGroupName is null &&
            _syncMultipleProjectsGitsParametersV2.ProjectName is null)
            projectsList = _syncMultipleProjectsGitsParametersV2.Projects;
        else if (_syncMultipleProjectsGitsParametersV2.ProjectGroupName is not null)
            projectsList = _syncMultipleProjectsGitsParametersV2.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                _syncMultipleProjectsGitsParametersV2.ProjectGroupName);
        else
            projectsList =
                _syncMultipleProjectsGitsParametersV2.Projects.Where(x =>
                    x.Key == _syncMultipleProjectsGitsParametersV2.ProjectName);

        var projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        var gitSyncToolsByGitProjectNames = new Dictionary<string, GitProjectSyncronizer>();


        foreach (var (projectName, project) in projectsListOrdered)
        foreach (var gitCol in Enum.GetValues<EGitCol>())
        foreach (var gitProjectName in project.GetGitProjectNames(gitCol))
        {
            if (!gitSyncToolsByGitProjectNames.ContainsKey(gitProjectName))
                gitSyncToolsByGitProjectNames.Add(gitProjectName,
                    new GitProjectSyncronizer(_logger, _parametersManager, gitProjectName, true));
            gitSyncToolsByGitProjectNames[gitProjectName].Add(projectName, gitCol);
        }

        string? usedCommitMessage = null;

        var useSameMessageForNextCommits = false;

        Console.WriteLine("Count changes");
        //წინასწარ ვადგენთ, რომელიმე რეპოზიტორიაში ხომ არ გვაქვს ცვლილებები, რომ პირველ რიგში ისინი დავამუშაოვოთ
        foreach (var keyValuePair in gitSyncToolsByGitProjectNames.Where(x => x.Value.Count > 0))
        {
            var syncer = keyValuePair.Value;
            syncer.CountHasChanges();
        }

        foreach (var keyValuePair in gitSyncToolsByGitProjectNames.Where(x => x.Value.Count > 0)
                     .OrderBy(x => x.Value.HasChanges ? 0 : 1).ThenBy(x => x.Key))
        {
            var syncer = keyValuePair.Value;
            syncer.UsedCommitMessage = usedCommitMessage;
            syncer.UseSameMessageForNextCommits = useSameMessageForNextCommits;
            syncer.RunSync();
            usedCommitMessage = syncer.UsedCommitMessage;
            useSameMessageForNextCommits = syncer.UseSameMessageForNextCommits;
        }

        return ValueTask.FromResult(true);
    }

}