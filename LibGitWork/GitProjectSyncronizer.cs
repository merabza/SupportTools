using System;
using LibGitData;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using LibDataInput;

namespace LibGitWork;

public class GitProjectSyncronizer
{
    private readonly List<GitSyncToolAction> _gitSyncToolActionList = [];
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _gitProjectName;

    public int Count =>_gitSyncToolActionList.Count;

    public string? UsedCommitMessage { get; private set; }


    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectSyncronizer(ILogger? logger, ParametersManager parametersManager, string gitProjectName,
        string? commitMessage = null)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _gitProjectName = gitProjectName;
        UsedCommitMessage = commitMessage;
    }

    public void Add(string projectName, EGitCol gitCol)
    {
        var gitSyncToolAction = GitSyncToolAction.Create(_logger, _parametersManager, projectName, gitCol,
            _gitProjectName, true);
        if (gitSyncToolAction is null)
            return;
        _gitSyncToolActionList.Add(gitSyncToolAction);
    }

    public void RunSync()
    {
        foreach (var gitSyncToolAction in _gitSyncToolActionList)
        {
            var (phase1Result, gitProcessor) = gitSyncToolAction.RunActionPhase1();
            
            //აქ გავჩერდი, თუ საჭირო იყო, კლონირება მოხდა, დადგინდა ცვლილებები დასაკომიტებელია თუ არა. ცნობილია მოხდა თუ არა შეცდომები

            //თუ დაიკლონა, მეტი აღარაფერი გვჭირდება
            if ( phase1Result == EFirstPhaseResult.Cloned )
                continue;

            //თუ პირველი ფაზის დროს მოხდა შეცდომა, ამ ფოლდერს თავს ვანებებთ
            if ( phase1Result == EFirstPhaseResult.FinishedWithErrors )
                continue;

            //მოშორებული ინფორმაციის განახლება
            //თუ განახლებისას მოხდა შეცდომა, ამ ფოლდერს თავს ვანებებთ
            if (!gitProcessor.GitRemoteUpdate())
                continue;

            if ( phase1Result == EFirstPhaseResult.NeedCommit )
            {
                UsedCommitMessage ??= Inputer.InputTextRequired("Message",
                    UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));
                if ( ! gitProcessor.Commit(UsedCommitMessage) )
                    continue;
            }


            ////აქედან მეთოდის ბოლომდე დასასრულებელია კოდი და მერე გასატესტია
            var local = gitProcessor.GitGetLocalId();
            if (local is null) 
                continue;

            var strBase = gitProcessor.GitGetBaseId();
            if (strBase is null) 
                continue;


            //gitProcessor.SyncRemote()
            if (local == strBase)
            {
                Console.WriteLine("need to pull");
                if (!gitProcessor.Pull())
                    continue;
            }

            var remote = gitProcessor.GitGetRemoteId();
            if (remote is null)
                continue;

            if (remote == strBase)
            {
                Console.WriteLine("need to push");
                if (!gitProcessor.Push())
                    continue;
            }



            //თუ დაკომიტება საჭიროა დაკომიტდეს და დასინქრონიზდეს.

            //თუ დაკომიტება საჭირო არაა დადგინდეს საჭიროა თუ არა სინქრონიზაცია და საჭიროების შემთხვევაში დასინქრონიზდეს.



            
        }

    }

}