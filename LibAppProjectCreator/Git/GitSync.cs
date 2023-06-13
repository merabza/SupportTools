using System;
using System.IO;
using LibDataInput;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Domain;
using SystemToolsShared;

namespace LibAppProjectCreator.Git;

public sealed class GitSync : ToolAction
{
    private readonly bool _askCommitMessage;
    private readonly GitDataDomain _gitData;
    private readonly string _gitsFolder;

    public GitSync(ILogger logger, string gitsFolder, GitDataDomain gitData, string? commitMessage = null,
        bool askCommitMessage = true) : base(logger, true, "Git Sync")
    {
        _gitsFolder = gitsFolder;
        _gitData = gitData;
        _askCommitMessage = askCommitMessage;
        UsedCommitMessage = commitMessage;
    }

    public string? UsedCommitMessage { get; private set; }

    protected override bool CheckValidate()
    {
        if (!string.IsNullOrWhiteSpace(_gitsFolder))
            return true;
        StShared.WriteErrorLine("Project Folder Name not found.", true);
        return false;
    }

    protected override bool RunAction()
    {
        var projectFolderName = Path.Combine(_gitsFolder, _gitData.GitProjectFolderName);
        var gitProcessor = new GitProcessor(true, Logger, projectFolderName);
        if (!Directory.Exists(projectFolderName))
            return gitProcessor.Clone(_gitData.GitProjectAddress);
        //თუ ფოლდერი არსებობს, მაშინ დადგინდეს
        //1. არის თუ არა გიტი ინიციალიზებულია ამ ფოლდერში
        //2. შეესაბამება თუ არა Git-ი პროექტის მისამართს. ანუ თავის დროზე ამ მისამართიდანაა დაკლონილი?
        // თუ რომელიმე არ სრულდება გამოვიდეს შესაბამისი შეტყობინება

        var gitInitialized = gitProcessor.IsGitInitialized();

        if (!gitInitialized)
        {
            StShared.WriteErrorLine(
                $"Git project folder exists, but not initialized. folder: {projectFolderName}.", true, Logger);
            return false;
        }

        var remoteOriginUrl = gitProcessor.GetRemoteOriginUrl();

        if (remoteOriginUrl != _gitData.GitProjectAddress)
        {
            StShared.WriteErrorLine($"Git is not valid. folder: {projectFolderName}.", true, Logger);
            return false;
        }

        var haveUnTrackedFiles = gitProcessor.HaveUnTrackedFiles();
        if (haveUnTrackedFiles)
            if (!gitProcessor.Add())
                return false;

        if (!gitProcessor.NeedCommit())
            return gitProcessor.SyncRemote();

        if (_askCommitMessage || UsedCommitMessage is null)
            UsedCommitMessage =
                Inputer.InputTextRequired("Message", UsedCommitMessage ?? DateTime.Now.ToString("yyyyMMddHHmm"));

        return gitProcessor.Commit(UsedCommitMessage) && gitProcessor.SyncRemote();

        //მოხდეს ამ Git პროექტის დაკლონვა
    }
}