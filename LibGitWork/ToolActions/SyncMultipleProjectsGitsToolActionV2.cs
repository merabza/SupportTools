using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork.Helpers;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using ToolsManagement.LibToolActions;

namespace LibGitWork.ToolActions;

public sealed class SyncMultipleProjectsGitsToolActionV2 : ToolAction
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
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        return new SyncMultipleProjectsGitsToolActionV2(loggerOrNull, parametersManager,
            syncMultipleProjectsGitsParametersV2, useConsole);
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

        IEnumerable<KeyValuePair<string, ProjectModel>> projectsList =
            GitProjectListHelper.CreateProjectsList(_syncMultipleProjectsGitsParametersV2.Projects,
                _syncMultipleProjectsGitsParametersV2.ProjectGroupName,
                _syncMultipleProjectsGitsParametersV2.ProjectName);

        List<KeyValuePair<string, ProjectModel>> projectsListOrdered = projectsList.OrderBy(o => o.Key).ToList();

        var gitSyncToolsByGitProjectNames = new Dictionary<string, GitProjectSyncronizer>();

        foreach ((string projectName, ProjectModel project) in projectsListOrdered)
        {
            foreach (EGitCol gitCol in Enum.GetValues<EGitCol>())
            {
                foreach (string gitProjectName in project.GetGitProjectNamesByGitCollectionType(gitCol))
                {
                    if (!gitSyncToolsByGitProjectNames.TryGetValue(gitProjectName, out GitProjectSyncronizer? value))
                    {
                        value = new GitProjectSyncronizer(_logger, _parametersManager, gitProjectName, true);
                        gitSyncToolsByGitProjectNames.Add(gitProjectName, value);
                    }

                    value.Add(projectName, gitCol);
                }
            }
        }

        string? usedCommitMessage = null;

        bool useSameMessageForNextCommits = false;

        Console.WriteLine("Count changes");
        //წინასწარ ვადგენთ, რომელიმე რეპოზიტორიაში ხომ არ გვაქვს ცვლილებები, რომ პირველ რიგში ისინი დავამუშაოვოთ
        foreach ((string key, GitProjectSyncronizer syncer) in gitSyncToolsByGitProjectNames.Where(x =>
                     x.Value.Count > 0))
            //Console.WriteLine($"for {key}");
        {
            if (syncer.CountHasChanges())
            {
                Console.WriteLine($"{key} has changes");
            }
        }

        Console.WriteLine("Count changes finished");

        foreach (KeyValuePair<string, GitProjectSyncronizer> keyValuePair in gitSyncToolsByGitProjectNames
                     .Where(x => x.Value.Count > 0).OrderBy(x => x.Value.HasChanges ? 0 : 1).ThenBy(x => x.Key))
        {
            GitProjectSyncronizer syncer = keyValuePair.Value;
            syncer.UsedCommitMessage = usedCommitMessage;
            syncer.UseSameMessageForNextCommits = useSameMessageForNextCommits;
            syncer.RunSync();
            usedCommitMessage = syncer.UsedCommitMessage;
            useSameMessageForNextCommits = syncer.UseSameMessageForNextCommits;
        }

        return ValueTask.FromResult(true);
    }
}
