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
        GitProcessor = new GitProcessor(true, _logger, _projectFolderName);
    }

    public string? LastRemoteId => GitProcessor.LastRemoteId;
    public GitProcessor GitProcessor { get; }

    public EFirstPhaseResult Phase1Result { get; private set; }

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


    public bool HasChanges()
    {
        if (!Directory.Exists(_projectFolderName))
            return false;

        var haveUnTrackedFilesResult = GitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsT0)
            return haveUnTrackedFilesResult.AsT0;
        Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
            GitSyncToolActionErrors.HaveUnTrackedFilesError));
        return false;
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
        StShared.ConsoleWriteInformationLine(_logger, true, "Checking {0}...", _projectFolderName);

        Phase1Result = EFirstPhaseResult.FinishedWithErrors;
        if (!Directory.Exists(_projectFolderName))
            if (GitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress))
            {
                Phase1Result = EFirstPhaseResult.Cloned;
                return true;
            }
            else
            {
                return false;
            }
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        var gitInitialized = GitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine($"Git project folder exists, but not initialized. folder: {_projectFolderName}.",
                true, _logger);
            return false;
        }

        var getRemoteOriginUrlResult = GitProcessor.GetRemoteOriginUrl();
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
        var getRedundantCachedFilesListResult = GitProcessor.GetRedundantCachedFilesList();
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
                !GitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
            return false;

        var haveUnTrackedFilesResult = GitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
                GitSyncToolActionErrors.HaveUnTrackedFilesError));
            return false;
        }

        var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;

        if (haveUnTrackedFiles && !GitProcessor.Add())
            return false;

        var needCommitResult = GitProcessor.NeedCommit();
        if (needCommitResult.IsT0)
        {
            Phase1Result = needCommitResult.AsT0 ? EFirstPhaseResult.NeedCommit : EFirstPhaseResult.NotNeedCommit;
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
        if (Phase1Result != EFirstPhaseResult.NeedCommit)
            return GitProcessor.SyncRemote().Item1;

        if (_askCommitMessage || UsedCommitMessage is null)
            UsedCommitMessage =
                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

        if (!GitProcessor.Commit(UsedCommitMessage))
            return false;

        Changed = true;

        return GitProcessor.SyncRemote().Item1;
    }
}