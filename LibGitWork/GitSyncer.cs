//using LibDataInput;
//using LibGitData;
//using LibGitWork.Errors;
//using LibGitWork.ToolCommandParameters;
//using LibParameters;
//using Microsoft.Extensions.Logging;
//using SupportToolsData.Models;
//using System;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using SystemToolsShared;
//using SystemToolsShared.Errors;

//namespace LibGitWork;

//public sealed class GitSyncer
//{
//    private readonly bool _askCommitMessage;
//    private readonly GitSyncParameters _gitSyncParameters;
//    private readonly ILogger? _logger;

//    // ReSharper disable once ConvertToPrimaryConstructor
//    public GitSyncer(ILogger? logger, GitSyncParameters gitSyncParameters, string? commitMessage = null,
//        bool askCommitMessage = true)
//    {
//        _logger = logger;
//        _gitSyncParameters = gitSyncParameters;
//        _askCommitMessage = askCommitMessage;
//        UsedCommitMessage = commitMessage;
//    }

//    public bool Changed { get; private set; }
//    public string? UsedCommitMessage { get; private set; }

//    public static GitSyncer? Create(ILogger logger, ParametersManager parametersManager,
//        string projectName, EGitCol gitCol, string gitProjectName, bool useConsole)
//    {
//        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
//        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
//        var gitSyncParameters = GitSyncParameters.Create(loggerOrNull,
//            supportToolsParameters, projectName, gitCol, gitProjectName, useConsole);

//        if (gitSyncParameters is not null)
//            return new GitSyncer(loggerOrNull, gitSyncParameters);

//        StShared.WriteErrorLine("GitSyncParameters is not created", true);
//        return null;
//    }

//    private bool CheckValidate()
//    {
//        if (!string.IsNullOrWhiteSpace(_gitSyncParameters.GitsFolder))
//            return true;
//        StShared.WriteErrorLine("Project Folder Name not found.", true);
//        return false;
//    }

//    private Task<bool> RunAction(CancellationToken cancellationToken = default)
//    {
//        var projectFolderName =
//            Path.Combine(_gitSyncParameters.GitsFolder, _gitSyncParameters.GitData.GitProjectFolderName);
//        var gitProcessor = new GitProcessor(true, _logger, projectFolderName);
//        if (!Directory.Exists(projectFolderName))
//            return Task.FromResult(gitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress));
//        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
//        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
//        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
//        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

//        var gitInitialized = gitProcessor.IsGitInitialized();

//        if (!gitInitialized)
//        {
//            StShared.WriteErrorLine(
//                $"Git project folder exists, but not initialized. folder: {projectFolderName}.", true, _logger);
//            return Task.FromResult(false);
//        }

//        var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
//        if (getRemoteOriginUrlResult.IsT1)
//        {
//            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
//                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
//            return Task.FromResult(false);
//        }

//        var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

//        if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
//        {
//            StShared.WriteErrorLine($"Git is not valid. folder: {projectFolderName}.", true, _logger);
//            return Task.FromResult(false);
//        }

//        //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
//        //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
//        var getRedundantCachedFilesListResult = gitProcessor.GetRedundantCachedFilesList();
//        if (getRedundantCachedFilesListResult.IsT1)
//        {
//            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRedundantCachedFilesListResult.AsT1,
//                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
//            return Task.FromResult(false);
//        }

//        var redundantCachedFilesList = getRedundantCachedFilesListResult.AsT0;

//        //და წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
//        //git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
//        if (redundantCachedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).Any(redundantCachedFileName =>
//                !gitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
//            return Task.FromResult(false);

//        var haveUnTrackedFilesResult = gitProcessor.HaveUnTrackedFiles();
//        if (haveUnTrackedFilesResult.IsT1)
//        {
//            Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
//                GitSyncToolActionErrors.HaveUnTrackedFilesError));
//            return Task.FromResult(false);
//        }

//        var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;

//        if (haveUnTrackedFiles)
//            if (!gitProcessor.Add())
//                return Task.FromResult(false);

//        var needCommitResult = gitProcessor.NeedCommit();
//        if (needCommitResult.IsT1)
//        {
//            Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1,
//                GitSyncToolActionErrors.NeedCommitError));
//            return Task.FromResult(false);
//        }

//        Changed = needCommitResult.AsT0;

//        if (!needCommitResult.AsT0)
//            return Task.FromResult(gitProcessor.SyncRemote());

//        if (_askCommitMessage || UsedCommitMessage is null)
//            UsedCommitMessage =
//                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

//        // ReSharper disable once using
//        var result = Task.FromResult(gitProcessor.Commit(UsedCommitMessage) && gitProcessor.SyncRemote());

//        return result;
//    }
//}


