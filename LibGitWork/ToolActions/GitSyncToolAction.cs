using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibDataInput;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibGitWork.ToolActions;

public sealed class GitSyncToolAction : ToolAction
{
    private readonly GitSyncParameters _gitSyncParameters;
    private readonly bool _askCommitMessage;
    public bool Changed { get; private set; }
    public string? UsedCommitMessage { get; private set; }


    public GitSyncToolAction(ILogger logger, GitSyncParameters gitSyncParameters, string? commitMessage = null,
        bool askCommitMessage = true) : base(logger, "Git Sync", null, null)
    {
        _gitSyncParameters = gitSyncParameters;
        _askCommitMessage = askCommitMessage;
        UsedCommitMessage = commitMessage;
    }

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
        var projectFolderName = Path.Combine(_gitSyncParameters.GitsFolder, _gitSyncParameters.GitData.GitProjectFolderName);
        var gitProcessor = new GitProcessor(true, Logger, projectFolderName);
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
                $"Git project folder exists, but not initialized. folder: {projectFolderName}.", true, Logger);
            return Task.FromResult(false);
        }

        var getRemoteOriginUrlResult = gitProcessor.GetRemoteOriginUrl();
        if (getRemoteOriginUrlResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(getRemoteOriginUrlResult.AsT1,
                new Err
                {
                    ErrorCode = "GetRemoteOriginUrlError", ErrorMessage = "Error when detecting Remote Origin Url"
                }));
            return Task.FromResult(false);
        }

        var remoteOriginUrl = getRemoteOriginUrlResult.AsT0;

        if (remoteOriginUrl != _gitSyncParameters.GitData.GitProjectAddress)
        {
            StShared.WriteErrorLine($"Git is not valid. folder: {projectFolderName}.", true, Logger);
            return Task.FromResult(false);
        }

        var haveUnTrackedFilesResult = gitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFilesResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(haveUnTrackedFilesResult.AsT1,
                new Err
                {
                    ErrorCode = "HaveUnTrackedFilesError", ErrorMessage = "Error when detecting UnTracked Files"
                }));
            return Task.FromResult(false);
        }

        var haveUnTrackedFiles = haveUnTrackedFilesResult.AsT0;
        if (haveUnTrackedFiles)
            if (!gitProcessor.Add())
                return Task.FromResult(false);

        var needCommitResult = gitProcessor.NeedCommit();
        if (needCommitResult.IsT1)
        {
            Err.PrintErrorsOnConsole(Err.RecreateErrors(needCommitResult.AsT1,
                new Err { ErrorCode = "NeedCommitError", ErrorMessage = "Error when detecting Need Commit" }));
            return Task.FromResult(false);
        }

        if (!needCommitResult.AsT0)
            return Task.FromResult(gitProcessor.SyncRemote());

        if (_askCommitMessage || UsedCommitMessage is null)
            UsedCommitMessage =
                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

        // ReSharper disable once using
        var result = Task.FromResult(gitProcessor.Commit(UsedCommitMessage) && gitProcessor.SyncRemote());
        Changed = true;
        return result;

    }
}