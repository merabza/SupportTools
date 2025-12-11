using System;
using LanguageExt;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemToolsShared;
using SystemToolsShared.Errors;

namespace LibGitWork;

public sealed class GitProcessor
{
    private const string Git = "git";
    private readonly ILogger? _logger;
    private readonly string _projectPath;
    private readonly string _switchToProjectPath;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProcessor(bool useConsole, ILogger? logger, string projectPath)
    {
        _useConsole = useConsole;
        _logger = logger;
        _projectPath = projectPath;
        _switchToProjectPath = $"-C {_projectPath}";
    }

    public string? LastRemoteId { get; private set; }

    public void CheckRemoteId()
    {
        LastRemoteId = GitGetRemoteId();
    }

    //public OneOf<bool, Err[]> NeedPull(bool updateRemote = false)
    //{
    //    if (updateRemote && !GitRemoteUpdate())
    //        return new[] { GitSyncToolActionErrors.CouldNotUpdateGitRemote };

    //    var local = GitGetLocalId();
    //    if (local is null)
    //        return new[] { GitSyncToolActionErrors.CouldNotGetGitLocalId };

    //    var remote = GitGetRemoteId();
    //    if (remote is null)
    //        return new[] { GitSyncToolActionErrors.CouldNotGetGitRemoteId };

    //    var strBase = GitGetBaseId();
    //    if (strBase is null)
    //        return new[] { GitSyncToolActionErrors.CouldNotGetGitBaseId };

    //    return local != remote && local == strBase;
    //}
    public GitState GetGitState()
    {
        //https://newbedev.com/check-if-pull-needed-in-git
        /*#!/bin/sh

UPSTREAM=${1:-'@{u}'}
LOCAL=$(git rev-parse @)
REMOTE=$(git rev-parse "$UPSTREAM")
BASE=$(git merge-base @ "$UPSTREAM")

if [ $LOCAL = $REMOTE ]; then
echo "Up-to-date"
elif [ $LOCAL = $BASE ]; then
echo "Need to pull"
elif [ $REMOTE = $BASE ]; then
echo "Need to push"
else
echo "Diverged"
fi*/
        //"git remote update"

        //if (!GitRemoteUpdate()) 
        //    return GitState.Unknown;

        var local = GitGetLocalId();
        if (local is null)
            return GitState.Unknown;

        LastRemoteId = GitGetRemoteId();
        if (LastRemoteId is null)
            return GitState.Unknown;

        var strBase = GitGetBaseId();
        if (strBase is null)
            return GitState.Unknown;

        if (local == LastRemoteId)
        {
            StShared.ConsoleWriteInformationLine(_logger, _useConsole, "{0} Up to date", _projectPath);
            return GitState.UpToDate;
        }

        if (local == strBase)
        {
            Console.WriteLine("need to pull");
            return GitState.NeedToPull;
        }

        if (LastRemoteId == strBase)
        {
            Console.WriteLine("need to push");
            return GitState.NeedToPush;
        }

        StShared.WriteWarningLine("Diverged", _useConsole, _logger);
        return GitState.NeedToPull;
    }

    private string? GitGetLocalId()
    {
        return GitGetId("rev-parse @");
    }

    private string? GitGetRemoteId()
    {
        return GitGetId("rev-parse @{u}");
    }

    private string? GitGetBaseId()
    {
        return GitGetId("merge-base @ @{u}");
    }

    private string? GitGetId(string parameters)
    {
        var localResult = StShared.RunProcessWithOutput(false, null, Git, $"{_switchToProjectPath} {parameters}");
        if (!localResult.IsT1)
            return localResult.AsT0.Item1;

        StShared.WriteErrorLine($"{Git} {parameters} Error", _useConsole, _logger);
        return null;
    }

    public bool GitRemoteUpdate()
    {
        if (!StShared.RunProcess(false, _logger, Git, $"{_switchToProjectPath} remote update").IsSome)
            return true;

        StShared.WriteErrorLine($"cannot run remote update for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Pull()
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} pull").IsNone)
            return true;

        StShared.WriteErrorLine("cannot pull", _useConsole, _logger);
        return false;
    }

    public OneOf<string, Err[]> GetRemoteOriginUrl()
    {
        var result = StShared.RunProcessWithOutput(false, null, Git,
            $"{_switchToProjectPath} config --get remote.origin.url");
        if (result.IsT1)
            return (Err[])result.AsT1;
        return result.AsT0.Item1.Trim(Environment.NewLine.ToCharArray());
    }

    public bool Commit(string commitMessage)
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} commit -m \"{commitMessage}\"")
            .IsNone)
            return true;

        StShared.WriteErrorLine($"cannot run commit for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public OneOf<bool, Err[]> NeedCommit()
    {
        var gitStatusOutputResult =
            StShared.RunProcessWithOutput(false, null, Git, $"{_switchToProjectPath} status --porcelain");
        if (gitStatusOutputResult.IsT1)
            return (Err[])gitStatusOutputResult.AsT1;
        var gitStatusOutput = gitStatusOutputResult.AsT0.Item1;
        return gitStatusOutput != string.Empty;
    }

    public bool Add()
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} add .").IsNone)
            return true;
        StShared.WriteErrorLine($"cannot run add for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Reset()
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} reset").IsNone)
            return true;
        StShared.WriteErrorLine($"cannot run reset for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Checkout()
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} checkout .").IsNone)
            return true;
        StShared.WriteErrorLine($"cannot run checkout for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Clean_fdx()
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} clean -fdx").IsNone)
            return true;
        StShared.WriteErrorLine($"cannot run clean -fdx for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public OneOf<bool, Err[]> HaveUnTrackedFiles()
    {
        //return !StShared.RunProcess(_useConsole, null, Git, $"{_switchToProjectPath} diff-files --quiet", false);
        var statusCommandOutputResult = StShared.RunProcessWithOutput(false, null, Git,
            $"{_switchToProjectPath} status --porcelain --untracked-files");

        if (statusCommandOutputResult.IsT1)
            return (Err[])statusCommandOutputResult.AsT1;
        var statusCommandOutput = statusCommandOutputResult.AsT0.Item1;

        return !string.IsNullOrWhiteSpace(statusCommandOutput);
    }

    public bool IsGitInitialized()
    {
        return StShared.RunProcess(false, _logger, Git, $"{_switchToProjectPath} rev-parse").IsNone;
    }

    private bool Push()
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} push").IsNone)
            return true;

        StShared.WriteErrorLine("cannot push", _useConsole, _logger);
        return false;
    }

    public bool Clone(string remoteAddress)
    {
        if (StShared.RunProcess(_useConsole, _logger, Git, $"clone {remoteAddress} {_projectPath}").IsNone)
        {
            CheckRemoteId();
            return true;
        }

        StShared.WriteErrorLine($"cannot clone {remoteAddress} to {_projectPath}", _useConsole, _logger);
        return false;
    }

    public (bool, bool) SyncRemote()
    {
        //https://newbedev.com/check-if-pull-needed-in-git
        /*#!/bin/sh

UPSTREAM=${1:-'@{u}'}
LOCAL=$(git rev-parse @)
REMOTE=$(git rev-parse "$UPSTREAM")
BASE=$(git merge-base @ "$UPSTREAM")

if [ $LOCAL = $REMOTE ]; then
echo "Up-to-date"
elif [ $LOCAL = $BASE ]; then
echo "Need to pull"
elif [ $REMOTE = $BASE ]; then
echo "Need to push"
else
echo "Diverged"
fi*/
        var pushed = false;

        if (LastRemoteId is null && !GitRemoteUpdate())
            return (false, pushed);

        while (true)
            switch (GetGitState())
            {
                case GitState.UpToDate:
                    return (true, pushed);
                case GitState.NeedToPull:
                    if (!Pull())
                        return (false, pushed);
                    break;
                case GitState.NeedToPush:
                    if (!Push())
                        return (false, pushed);
                    pushed = true;
                    break;
                case GitState.Diverged:
                    StShared.WriteErrorLine($"{_projectPath} Diverged", _useConsole, _logger);
                    return (false, pushed);
                case GitState.Unknown:
                    StShared.WriteErrorLine($"{_projectPath} Unknown state", _useConsole, _logger);
                    return (false, pushed);
                default:
                    throw new ArgumentOutOfRangeException();
            }
    }

    //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
    //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
    public OneOf<string[], Err[]> GetRedundantCachedFilesList()
    {
        //return !StShared.RunProcess(_useConsole, null, Git, $"{_switchToProjectPath} diff-files --quiet", false);
        var statusCommandOutputResult = StShared.RunProcessWithOutput(false, null, Git,
            $"{_switchToProjectPath} ls-files -i --exclude-from=.gitignore -c");

        if (statusCommandOutputResult.IsT1)
            return (Err[])statusCommandOutputResult.AsT1;
        var statusCommandOutput = statusCommandOutputResult.AsT0.Item1;

        return string.IsNullOrWhiteSpace(statusCommandOutput) ? [] : statusCommandOutput.Split(Environment.NewLine);
    }

    //წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
    //git -C {GitPatch} rm --cached {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად, ანუ GitPatch-დან დაწყებული}
    public bool RemoveFromCacheRedundantCachedFile(string redundantCachedFileName)
    {
        if (StShared.RunProcess(_useConsole, _logger, Git,
                $"{_switchToProjectPath} rm --cached \"{redundantCachedFileName}\"").IsNone)
            return true;
        StShared.WriteErrorLine($"cannot remove file {redundantCachedFileName} from cache", _useConsole, _logger);
        return false;
    }

    public Option<Err[]> Initialise()
    {
        return StShared.RunProcess(_useConsole, _logger, Git, $"{_switchToProjectPath} init");
    }

    public bool IsFolderPartOfGitWorkingTree(string appFolderForDiffFullName)
    {
        var isInsideWorkTreeResult = StShared.RunProcessWithOutput(false, _logger, Git,
            $"-C \"{appFolderForDiffFullName}\" rev-parse --is-inside-work-tree", [128]);
        if (isInsideWorkTreeResult.IsT1)
            return false;
        var isInsideWorkTree = isInsideWorkTreeResult.AsT0;

        return isInsideWorkTree.Item2 == 0 && isInsideWorkTree.Item1 == "true" + Environment.NewLine;
    }

    /*
        var isInsideWorkTreeResult = StShared.RunProcessWithOutput(false, _logger, "git",
           $"-C \"{appFolderForDiffFullName}\" rev-parse --is-inside-work-tree", [128]);
     */
}