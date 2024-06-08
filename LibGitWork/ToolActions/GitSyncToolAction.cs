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

// ReSharper disable ConvertToPrimaryConstructor

namespace LibGitWork.ToolActions;

public sealed class GitSyncToolAction : ToolAction
{
    private readonly bool _askCommitMessage;
    private readonly GitSyncParameters _gitSyncParameters;
    private readonly ILogger _logger;


    public GitSyncToolAction(ILogger logger, GitSyncParameters gitSyncParameters, string? commitMessage = null,
        bool askCommitMessage = true) : base(logger, "Git Sync", null, null)
    {
        _logger = logger;
        _gitSyncParameters = gitSyncParameters;
        _askCommitMessage = askCommitMessage;
        UsedCommitMessage = commitMessage;
    }

    public bool Changed { get; private set; }
    public string? UsedCommitMessage { get; private set; }

    public static GitSyncToolAction? Create(ILogger logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol, string gitProjectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var gitSyncParameters =
            GitSyncParameters.Create(logger, supportToolsParameters, projectName, gitCol, gitProjectName);

        if (gitSyncParameters is not null)
            return new GitSyncToolAction(logger, gitSyncParameters);

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

    protected override Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var projectFolderName =
            Path.Combine(_gitSyncParameters.GitsFolder, _gitSyncParameters.GitData.GitProjectFolderName);
        var gitProcessor = new GitProcessor(true, _logger, projectFolderName);
        if (!Directory.Exists(projectFolderName))
            return Task.FromResult(gitProcessor.Clone(_gitSyncParameters.GitData.GitProjectAddress));
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        var gitInitialized = gitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine(
                $"Git project folder exists, but not initialized. folder: {projectFolderName}.", true, _logger);
            return Task.FromResult(false);
        }

        var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
        if (getRemoteOriginUrlResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
            return Task.FromResult(false);
        }

        var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

        if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
        {
            StShared.WriteErrorLine($"Git is not valid. folder: {projectFolderName}.", true, _logger);
            return Task.FromResult(false);
        }

        //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
        //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
        var getRedundantCachedFilesListResult = gitProcessor.GetRedundantCachedFilesList();
        if (getRedundantCachedFilesListResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRedundantCachedFilesListResult.AsT1,
                GitSyncToolActionErrors.GetRedundantCachedFilesListError));
            return Task.FromResult(false);
        }

        var redundantCachedFilesList = getRedundantCachedFilesListResult.AsT0;

        //და წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
        //git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
        if (redundantCachedFilesList.Where(x => !string.IsNullOrWhiteSpace(x)).Any(redundantCachedFileName =>
                !gitProcessor.RemoveFromCacheRedundantCachedFile(redundantCachedFileName)))
            return Task.FromResult(false);

        var haveUnTrackedFilesResult = gitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
                GitSyncToolActionErrors.HaveUnTrackedFilesError));
            return Task.FromResult(false);
        }

        var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;

        if (haveUnTrackedFiles)
            //შემდეგი კოდი დავაკომენტარე, რადგან ის ამოწმებდა შეიცვალა თუ არა .gitignore. პრინციპში ასეთი შემოწმება საჭირო არც არის,
            //  რადგან პირდაპირ შეგვიძლია არასაჭირო ფაილების ქეშიდან გასუფთავება
            //თუ .gitignore ფაილი არ შეიცვალა, არც ზედმეტად ქშირებული ფაილები არ უნდა იყოს, მაგრამ მაინც თავს ვიზღვევთ
            //იმისათვის, რომ დავადგინოთ .gitignore ფაილი შეიცვალა თუ არა, უნდა გავუშვათ შემდეგი ბრძანება:
            //git diff --name-only .gitignore
            //var isGitIgnoreFileChangedResult = gitProcessor.IsGitIgnoreFileChanged();
            //if (isGitIgnoreFileChangedResult.IsT1)
            //{
            //    Err.PrintErrorsOnConsole(Err.RecreateErrors(isGitIgnoreFileChangedResult.AsT1,
            //        new Err
            //        {
            //            ErrorCode = "IsGitIgnoreFileChangedError", ErrorMessage = "Error when detecting Is .gitignore File Changed"
            //        }));
            //    return Task.FromResult(false);
            //}
            //var isGitIgnoreFileChanged = isGitIgnoreFileChangedResult.AsT0;
            //if (!isGitIgnoreFileChanged)
            //{
            //}
            if (!gitProcessor.Add())
                return Task.FromResult(false);


        var needCommitResult = gitProcessor.NeedCommit();
        if (needCommitResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1,
                GitSyncToolActionErrors.NeedCommitError));
            return Task.FromResult(false);
        }

        Changed = needCommitResult.AsT0;

        if (!needCommitResult.AsT0)
            return Task.FromResult(gitProcessor.SyncRemote());

        if (_askCommitMessage || UsedCommitMessage is null)
            UsedCommitMessage =
                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

        // ReSharper disable once using
        var result = Task.FromResult(gitProcessor.Commit(UsedCommitMessage) && gitProcessor.SyncRemote());

        return result;
    }
}