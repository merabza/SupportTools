using System;
using System.Collections.Generic;
using System.Linq;
using LibDataInput;
using LibGitData;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork;

public sealed class GitProjectSyncronizer
{
    private readonly string _gitProjectName;
    private readonly List<GitSyncToolAction> _gitSyncToolActionList = [];
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectSyncronizer(ILogger? logger, ParametersManager parametersManager, string gitProjectName,
        bool useConsole)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _gitProjectName = gitProjectName;
        _useConsole = useConsole;
    }

    public bool HasChanges { get; private set; }

    public string? UsedCommitMessage { get; set; }

    public bool UseSameMessageForNextCommits { get; set; }

    public int Count => _gitSyncToolActionList.Count;

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
        //ეს ნაწილი არის SupportTools-ის სამუშაო ფოლდერში არსებული კლონის გაახლება
        var gitProjectsUpdater = GitProjectsUpdater.Create(_logger, _parametersManager, _gitProjectName, _useConsole);
        if (gitProjectsUpdater is null)
        {
            StShared.WriteErrorLine("gitProjectsUpdater does not created", true, _logger);
            return;
        }

        var haveSameChanges = false;
        var loopNom = 0;

        var gitCollect = EGitCollect.Collect;
        while (gitCollect == EGitCollect.Collect || haveSameChanges)
        {
            haveSameChanges = false;
            Console.WriteLine(
                $"---=== {_gitProjectName} {gitCollect} {(loopNom == 0 ? string.Empty : loopNom)} ===---");

            var gitProjectsUpdaterGitProcessor = gitProjectsUpdater.ProcessOneGitProject(false);

            if (gitProjectsUpdaterGitProcessor is null)
            {
                StShared.WriteErrorLine("ProcessOneGitProject is not working", true, _logger);
                return;
            }

            var lastRemoteId = gitProjectsUpdater.LastRemoteId;

            //SupportTools-ის სამუშაო ფოლდერში არსებული კლონის გაახლება დასრულდა აქ
            var pushed = false;
            foreach (var gitSyncToolAction in _gitSyncToolActionList)
            {
                //var committed = false;
                if (gitCollect == EGitCollect.Collect)
                {
                    if (!gitSyncToolAction.RunActionPhase1())
                        continue;

                    switch (gitSyncToolAction.Phase1Result)
                    {
                        case EFirstPhaseResult.Cloned:
                            continue;
                        case EFirstPhaseResult.NeedCommit:
                        {
                            if (!UseSameMessageForNextCommits || UsedCommitMessage is null)
                            {
                                UsedCommitMessage ??= Inputer.InputTextRequired("Message",
                                    UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

                                UseSameMessageForNextCommits =
                                    Inputer.InputBool("Use this message for next commits?", true);
                            }

                            if (!gitSyncToolAction.GitProcessor.Commit(UsedCommitMessage))
                                continue;

                            haveSameChanges = true;
                            //committed = true;
                            break;
                        }
                        case EFirstPhaseResult.FinishedWithErrors:
                            continue;
                        case EFirstPhaseResult.NotNeedCommit:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                gitSyncToolAction.GitProcessor.CheckRemoteId();

                if (pushed || gitSyncToolAction.LastRemoteId != lastRemoteId)
                {
                    //მოშორებული ინფორმაციის განახლება
                    //თუ განახლებისას მოხდა შეცდომა, ამ ფოლდერს თავს ვანებებთ
                    if (!gitSyncToolAction.GitProcessor.GitRemoteUpdate())
                        continue;

                    gitSyncToolAction.GitProcessor.CheckRemoteId();
                    lastRemoteId = gitSyncToolAction.LastRemoteId;
                }

                (_, pushed) = gitSyncToolAction.GitProcessor.SyncRemote();
            }

            gitCollect = EGitCollect.Usage;
            loopNom++;

            Console.WriteLine("---===---------===---");
        }
    }

    public void CountHasChanges()
    {
        HasChanges = _gitSyncToolActionList.Any(gitSyncToolAction => gitSyncToolAction.HasChanges());
    }
}