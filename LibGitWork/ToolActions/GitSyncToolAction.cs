using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.LibDataInput;
using LibGitData;
using LibGitWork.Errors;
using LibGitWork.ToolCommandParameters;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.BackgroundTasks;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

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
        GitProcessor = new GitProcessor(true, _logger, _projectFolderName, _gitSyncParameters.GitExecutablePath);
    }

    public string? LastRemoteId => GitProcessor.LastRemoteId;
    public GitProcessor GitProcessor { get; }

    public EFirstPhaseResult Phase1Result { get; private set; }

    public bool Changed { get; private set; }
    public string? UsedCommitMessage { get; private set; }

    public static GitSyncToolAction? Create(ILogger? logger, IParametersManager parametersManager, string projectName,
        EGitCol gitCol, string gitProjectName, bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        ILogger? loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var gitSyncParameters = GitSyncParameters.Create(loggerOrNull, supportToolsParameters, projectName, gitCol,
            gitProjectName, useConsole);

        if (gitSyncParameters is not null)
        {
            return new GitSyncToolAction(loggerOrNull, gitSyncParameters);
        }

        StShared.WriteErrorLine("GitSyncParameters is not created", true);
        return null;
    }

    public bool HasChanges()
    {
        if (!Directory.Exists(_projectFolderName))
        {
            return false;
        }

        Result<bool> haveUnTrackedFilesResult = GitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsSuccess)
        {
            return haveUnTrackedFilesResult.Value;
        }

        Result.CreateValidationError([
            .. haveUnTrackedFilesResult.Error.ToErrorArray(),
            GitSyncToolActionErrors.HaveUnTrackedFilesError
        ]).PrintErrorsOnConsole();
        return false;
    }

    protected override bool CheckValidate()
    {
        if (!string.IsNullOrWhiteSpace(_gitSyncParameters.GitsFolder))
        {
            return true;
        }

        StShared.WriteErrorLine("Project Folder Name not found.", true);
        return false;
    }

    public bool RunActionPhase1() //GitProcessor? gitProcessor = null
    {
        StShared.ConsoleWriteInformationLine(_logger, true, "Checking {0}...", _projectFolderName);

        Phase1Result = EFirstPhaseResult.FinishedWithErrors;
        if (!Directory.Exists(_projectFolderName))
        {
            if (GitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress))
            {
                Phase1Result = EFirstPhaseResult.Cloned;
                return true;
            }

            return false;
        }
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        bool gitInitialized = GitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine($"Git project folder exists, but not initialized. folder: {_projectFolderName}.",
                true, _logger);
            return false;
        }

        Result<string> getRemoteOriginUrlResult = GitProcessor.GetRemoteOriginUrl();
        if (getRemoteOriginUrlResult.IsFailure)
        {
            Result.CreateValidationError([
                .. getRemoteOriginUrlResult.Error.ToErrorArray(),
                GitSyncToolActionErrors.GetRedundantCachedFilesListError
            ]).PrintErrorsOnConsole();
            return false;
        }

        string remoteOriginUrl = getRemoteOriginUrlResult.Value;

        if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
        {
            StShared.WriteErrorLine($"Git is not valid. folder: {_projectFolderName}.", true, _logger);
            return false;
        }

        //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
        //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
        Result<string[]> getRedundantCachedFilesListResult = GitProcessor.GetRedundantCachedFilesList();
        if (getRedundantCachedFilesListResult.IsFailure)
        {
            Result.CreateValidationError([
                .. getRedundantCachedFilesListResult.Error.ToErrorArray(),
                GitSyncToolActionErrors.GetRedundantCachedFilesListError
            ]).PrintErrorsOnConsole();
            return false;
        }

        string[] redundantCachedFilesList = getRedundantCachedFilesListResult.Value;

        //და წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
        //git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
        if (redundantCachedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).Any(redundantCachedFileName =>
                !GitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
        {
            return false;
        }

        Result<bool> haveUnTrackedFilesResult = GitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsFailure)
        {
            Result.CreateValidationError([
                .. haveUnTrackedFilesResult.Error.ToErrorArray(),
                GitSyncToolActionErrors.HaveUnTrackedFilesError
            ]).PrintErrorsOnConsole();
            return false;
        }

        bool haveUnTrackedFiles = haveUnTrackedFilesResult.Value;

        if (haveUnTrackedFiles && !GitProcessor.Add())
        {
            return false;
        }

        Result<bool> needCommitResult = GitProcessor.NeedCommit();
        if (needCommitResult.IsSuccess)
        {
            Phase1Result = needCommitResult.Value ? EFirstPhaseResult.NeedCommit : EFirstPhaseResult.NotNeedCommit;
            return true;
        }

        Result.CreateValidationError([
            .. needCommitResult.Error.ToErrorArray(),
            GitSyncToolActionErrors.NeedCommitError
        ]).PrintErrorsOnConsole();
        return false;
    }

    protected override ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(RunActionPhase1() && RunActionPhase2());
    }

    private bool RunActionPhase2()
    {
        //თუ ცვლილებები არის, მაშინ ჯერ ვაკეთებთ ქომიტს და შემდეგ სინქრონიზაციას
        if (Phase1Result != EFirstPhaseResult.NeedCommit)
        {
            return GitProcessor.SyncRemote().Item1;
        }

        if (_askCommitMessage || UsedCommitMessage is null)
        {
            UsedCommitMessage = Inputer.InputTextRequired("Message",
                UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture));
        }

        if (!GitProcessor.Commit(UsedCommitMessage))
        {
            return false;
        }

        Changed = true;

        return GitProcessor.SyncRemote().Item1;
    }
}
