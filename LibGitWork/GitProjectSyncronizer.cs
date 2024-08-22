using System;
using System.Collections.Generic;
using LibDataInput;
using LibGitData;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork;

public class GitProjectSyncronizer
{
    private readonly string _gitProjectName;
    private readonly List<GitSyncToolAction> _gitSyncToolActionList = [];
    private readonly ILogger? _logger;
    private readonly ParametersManager _parametersManager;
    private readonly bool _useConsole;

    private string? _usedCommitMessage;


    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectSyncronizer(ILogger? logger, ParametersManager parametersManager, string gitProjectName,
        bool useConsole, string? commitMessage = null)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _gitProjectName = gitProjectName;
        _useConsole = useConsole;
        _usedCommitMessage = commitMessage;
    }

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

        if (!gitProjectsUpdater.ProcessOneGitProject(false))
        {
            StShared.WriteErrorLine("ProcessOneGitProject is not working", true, _logger);
            return;
        }

        var lastRemoteId = gitProjectsUpdater.LastRemoteId;

        //SupportTools-ის სამუშაო ფოლდერში არსებული კლონის გაახლება დასრულდა აქ

        foreach (var gitCollect in Enum.GetValues<EGitCollect>())
        foreach (var gitSyncToolAction in _gitSyncToolActionList)
        {
            if (gitCollect == EGitCollect.Collect)
            {
                if (!gitSyncToolAction.RunActionPhase1())
                    continue;

                if (gitSyncToolAction.Phase1Result == EFirstPhaseResult.Cloned)
                    continue;

                if (gitSyncToolAction.Phase1Result == EFirstPhaseResult.NeedCommit)
                {
                    _usedCommitMessage ??= Inputer.InputTextRequired("Message",
                        _usedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

                    if (!gitSyncToolAction.GitProcessor.Commit(_usedCommitMessage)) continue;
                }
            }

            gitSyncToolAction.GitProcessor.CheckRemoteId();

            if (gitSyncToolAction.LastRemoteId != lastRemoteId)
            {
                //მოშორებული ინფორმაციის განახლება
                //თუ განახლებისას მოხდა შეცდომა, ამ ფოლდერს თავს ვანებებთ
                if (!gitSyncToolAction.GitProcessor.GitRemoteUpdate()) continue;

                gitSyncToolAction.GitProcessor.CheckRemoteId();

                lastRemoteId = gitSyncToolAction.LastRemoteId;
            }

            gitSyncToolAction.GitProcessor.SyncRemote();
        }
    }
}