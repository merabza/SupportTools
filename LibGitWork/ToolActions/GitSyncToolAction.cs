using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibDataInput;
using LibGitData;
using LibGitWork.Errors;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibGitWork.ToolActions;

public sealed class GitSyncToolAction : ToolAction
{
    private readonly bool _askCommitMessage;
    private readonly GitSyncParameters _gitSyncParameters;
    private readonly ILogger? _logger;
    private readonly string _projectFolderName;
    private readonly GitProcessor _gitProcessor;
    private EFirstPhaseResult _phase1Result;

    public string? LastRemoteId => _gitProcessor.LastRemoteId;
    public GitProcessor GitProcessor => _gitProcessor;
    public EFirstPhaseResult Phase1Result => _phase1Result;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSyncToolAction(ILogger? logger, GitSyncParameters gitSyncParameters, string? commitMessage = null,
        bool askCommitMessage = true) : base(logger, "Git Sync", null, null)
    {
        _logger = logger;
        _gitSyncParameters = gitSyncParameters;
        _askCommitMessage = askCommitMessage;
        UsedCommitMessage = commitMessage;
        _projectFolderName =
            Path.Combine(_gitSyncParameters.GitsFolder, _gitSyncParameters.GitData.GitProjectFolderName);
        _gitProcessor = new GitProcessor(true, _logger, _projectFolderName);
    }

    public bool Changed { get; private set; }
    public string? UsedCommitMessage { get; private set; }

    public static GitSyncToolAction? Create(ILogger? logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol, string gitProjectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var gitSyncParameters = GitSyncParameters.Create(loggerOrNull,
            supportToolsParameters, projectName, gitCol, gitProjectName, useConsole);

        if (gitSyncParameters is not null)
            return new GitSyncToolAction(loggerOrNull, gitSyncParameters);

        StShared.WriteErrorLine("GitSyncParameters is not created", true);
        return null;
    }


    protected override bool CheckValidate()
    {
        if (!string.IsNullOrWhiteSpace(_gitSyncParameters.GitsFolder))
            return true;
        StShared.WriteErrorLine("Project Folder Name not found.", true);
        return false;
    }


    public bool RunActionPhase1() //GitProcessor? gitProcessor = null
    {
        _phase1Result = EFirstPhaseResult.FinishedWithErrors;
        if (!Directory.Exists(_projectFolderName))
            if (_gitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress))
            {
                _phase1Result = EFirstPhaseResult.Cloned;
                return true;
            }   
            else
                return false;
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        var gitInitialized = _gitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine($"Git project folder exists, but not initialized. folder: {_projectFolderName}.",
                true, _logger);
            return false;
        }

        var getRemoteOriginUrlResult = _gitProcessor.GetRemoteOriginUrl();
        if (getRemoteOriginUrlResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
            return false;
        }

        var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

        if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
        {
            StShared.WriteErrorLine($"Git is not valid. folder: {_projectFolderName}.", true, _logger);
            return false;
        }

        //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
        //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
        var getRedundantCachedFilesListResult = _gitProcessor.GetRedundantCachedFilesList();
        if (getRedundantCachedFilesListResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRedundantCachedFilesListResult.AsT1,
                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
            return false;
        }

        var redundantCachedFilesList = getRedundantCachedFilesListResult.AsT0;

        //და წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
        //git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
        if (redundantCachedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).Any(redundantCachedFileName =>
                !_gitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
            return false;

        var haveUnTrackedFilesResult = _gitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
                GitSyncToolActionErrors.HaveUnTrackedFilesError));
            return false;
        }

        var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;

        if (haveUnTrackedFiles && !_gitProcessor.Add())
            return false;

        var needCommitResult = _gitProcessor.NeedCommit();
        if (needCommitResult.IsT0)
        {
            _phase1Result = needCommitResult.AsT0 ? EFirstPhaseResult.NeedCommit : EFirstPhaseResult.NotNeedCommit;
            return true;
        }

        Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1, GitSyncToolActionErrors.NeedCommitError));
        return false;

    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        RunActionPhase1();

        return Task.FromResult(RunActionPhase2());
    }

    private bool RunActionPhase2()
    {
        //თუ ცვლილებები არის, მაშინ ჯერ ვაკეთებთ ქომიტს და შემდეგ სინქრონიზაციას
        if (_phase1Result != EFirstPhaseResult.NeedCommit) 
            return _gitProcessor.SyncRemote();

        if (_askCommitMessage || UsedCommitMessage is null)
            UsedCommitMessage =
                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

        if (!_gitProcessor.Commit(UsedCommitMessage))
            return false;

        Changed = true;

        return _gitProcessor.SyncRemote();
    }
}