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

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
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
        {
            foreach (var gitCol in Enum.GetValues<EGitCol>())
            {
                foreach (var gitProjectName in project.GetGitProjectNames(gitCol))
                {
                    if (!gitSyncToolsByGitProjectNames.ContainsKey(gitProjectName))
                        gitSyncToolsByGitProjectNames.Add(gitProjectName,
                            new GitProjectSyncronizer(_logger, _parametersManager, gitProjectName, true));
                    gitSyncToolsByGitProjectNames[gitProjectName].Add(projectName, gitCol);
                }
            }
        }

        foreach (var keyValuePair in gitSyncToolsByGitProjectNames.Where(x => x.Value.Count > 0))
        {
            keyValuePair.Value.RunSync();
        }








        //var changedGitProjects = new Dictionary<EGitCollect, Dictionary<string, List<string>>>
        //{
        //    [EGitCollect.Collect] = [],
        //    [EGitCollect.Usage] = []
        //};
        
        //var loopNom = 0;
        //var gitCollectUsage = EGitCollect.Collect;

        //while (gitCollectUsage == EGitCollect.Collect || changedGitProjects[EGitCollect.Collect].Count > 0)
        //{
        //    changedGitProjects[EGitCollect.Collect] = [];
        //    Console.WriteLine($"---=== {gitCollectUsage} {(loopNom == 0 ? string.Empty : loopNom)} ===---");
        //    //პროექტების ჩამონათვალი
        //    foreach (var (projectName, project) in projectsListOrdered)
        //    {
        //        SyncAllGitsForOneProject(projectName, project, EGitCol.Main, changedGitProjects, loopNom == 0);
        //        if (_syncMultipleProjectsGitsParametersV2.ScaffoldSeedersWorkFolder is not null)
        //            SyncAllGitsForOneProject(projectName, project, EGitCol.ScaffoldSeed, changedGitProjects,
        //                loopNom == 0);
        //    }

        //    Console.WriteLine("---===---------===---");

        //    gitCollectUsage = EGitCollect.Usage;
        //    loopNom++;
        //    changedGitProjects[EGitCollect.Usage] = changedGitProjects[EGitCollect.Collect];
        //}

        return Task.FromResult(true);
    }

    //private void SyncAllGitsForOneProject(string projectName, ProjectModel project, EGitCol gitCol,
    //    Dictionary<EGitCollect, Dictionary<string, List<string>>> changedGitProjects, bool isFirstSync)
    //{
    //    if (!GitStat.CheckGitProject(projectName, project, gitCol))
    //        return;

    //    var syncAllGitsCliMenuCommandMain = SyncOneProjectAllGitsToolAction.Create(_logger, _parametersManager,
    //        projectName, gitCol, changedGitProjects, isFirstSync, UseConsole);
    //    syncAllGitsCliMenuCommandMain?.Run(CancellationToken.None).Wait();
    //}
}