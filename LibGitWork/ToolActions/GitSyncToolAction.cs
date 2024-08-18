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

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSyncToolAction(ILogger? logger, GitSyncParameters gitSyncParameters, string? commitMessage = null,
        bool askCommitMessage = true) : base(logger, "Git Sync", null, null)
    {
        _logger = logger;
        _gitSyncParameters = gitSyncParameters;
        _askCommitMessage = askCommitMessage;
        UsedCommitMessage = commitMessage;
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

    public (EFirstPhaseResult, GitProcessor) RunActionPhase1()
    {
        var projectFolderName =
            Path.Combine(_gitSyncParameters.GitsFolder, _gitSyncParameters.GitData.GitProjectFolderName);

        var gitProcessor = new GitProcessor(true, _logger, projectFolderName);
        if (!Directory.Exists(projectFolderName))
            return (
                gitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress)
                    ? EFirstPhaseResult.Cloned
                    : EFirstPhaseResult.FinishedWithErrors, gitProcessor);
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        var gitInitialized = gitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine($"Git project folder exists, but not initialized. folder: {projectFolderName}.",
                true, _logger);
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);
        }

        var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
        if (getRemoteOriginUrlResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);
        }

        var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

        if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
        {
            StShared.WriteErrorLine($"Git is not valid. folder: {projectFolderName}.", true, _logger);
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);
        }

        //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
        //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
        var getRedundantCachedFilesListResult = gitProcessor.GetRedundantCachedFilesList();
        if (getRedundantCachedFilesListResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRedundantCachedFilesListResult.AsT1,
                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);
        }

        var redundantCachedFilesList = getRedundantCachedFilesListResult.AsT0;

        //და წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
        //git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
        if (redundantCachedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).Any(redundantCachedFileName =>
                !gitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);

        var haveUnTrackedFilesResult = gitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
                GitSyncToolActionErrors.HaveUnTrackedFilesError));
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);
        }

        var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;

        if (haveUnTrackedFiles && !gitProcessor.Add())
            return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);

        var needCommitResult = gitProcessor.NeedCommit();
        if (needCommitResult.IsT0)
            return (needCommitResult.AsT0 ? EFirstPhaseResult.NeedCommit : EFirstPhaseResult.NotNeedCommit, gitProcessor);

        Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1, GitSyncToolActionErrors.NeedCommitError));
        return (EFirstPhaseResult.FinishedWithErrors, gitProcessor);

    }

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var (phase1Result, gitProcessor) = RunActionPhase1();


        //var projectFolderName =
        //    Path.Combine(_gitSyncParameters.GitsFolder, _gitSyncParameters.GitData.GitProjectFolderName);

        //var gitProcessor = new GitProcessor(true, _logger, projectFolderName);
        //if (!Directory.Exists(projectFolderName))
        //    return Task.FromResult(gitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress));
        ////თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        ////1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        ////2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        //// თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        //var gitInitialized = gitProcessor.IsGitInitialized();

        //if (!gitInitialized)
        //{
        //    StShared.WriteErrorLine(
        //        $"Git project folder exists, but not initialized. folder: {projectFolderName}.", true, _logger);
        //    return Task.FromResult(false);
        //}

        //var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
        //if (getRemoteOriginUrlResult.IsT1)
        //{
        //    Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
        //        GitSyncToolActionErrors.GetRedundantCachedFilesListError));
        //    return Task.FromResult(false);
        //}

        //var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

        //if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
        //{
        //    StShared.WriteErrorLine($"Git is not valid. folder: {projectFolderName}.", true, _logger);
        //    return Task.FromResult(false);
        //}

        ////ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
        ////git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
        //var getRedundantCachedFilesListResult = gitProcessor.GetRedundantCachedFilesList();
        //if (getRedundantCachedFilesListResult.IsT1)
        //{
        //    Err.PrintErrorsOnConsole(Err.RecreateErrors(getRedundantCachedFilesListResult.AsT1,
        //        GitSyncToolActionErrors.GetRedundantCachedFilesListError));
        //    return Task.FromResult(false);
        //}

        //var redundantCachedFilesList = getRedundantCachedFilesListResult.AsT0;

        ////და წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
        ////git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
        //if (redundantCachedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).Any(redundantCachedFileName =>
        //        !gitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
        //    return Task.FromResult(false);

        //var haveUnTrackedFilesResult = gitProcessor.HaveUnTrackedFiles();
        //if (haveUnTrackedFilesResult.IsT1)
        //{
        //    Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
        //        GitSyncToolActionErrors.HaveUnTrackedFilesError));
        //    return Task.FromResult(false);
        //}

        //var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;

        //if (haveUnTrackedFiles)
        //    if (!gitProcessor.Add())
        //        return Task.FromResult(false);


        //var needCommitResult = gitProcessor.NeedCommit();
        //if (needCommitResult.IsT1)
        //{
        //    Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1,
        //        GitSyncToolActionErrors.NeedCommitError));
        //    return Task.FromResult(false);
        //}

        //Changed = needCommitResult.AsT0;

        if (phase1Result == EFirstPhaseResult.NotNeedCommit) //თუ ცვლილებები არ არის, მაშინ ქომიტის გარეშე გაკეთდეს სინქრონიზაცია
            return Task.FromResult(gitProcessor.SyncRemote());

        //თუ ცვლილებები არის, მაშინ ჯერ ვაკეთებთ ქომიტს და შემდეგ სინქრონიზაციას

        if (_askCommitMessage || UsedCommitMessage is null)
            UsedCommitMessage =
                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

        Changed = true;

        // ReSharper disable once using
        var result = Task.FromResult(gitProcessor.Commit(UsedCommitMessage) && gitProcessor.SyncRemote());

        return result;
    }
}