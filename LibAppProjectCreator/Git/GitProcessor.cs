using System;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppProjectCreator.Git;

public sealed class GitProcessor
{
    private readonly ILogger _logger;
    private readonly string _projectPath;
    private readonly bool _useConsole;

    public GitProcessor(bool useConsole, ILogger logger, string projectPath)
    {
        _useConsole = useConsole;
        _logger = logger;
        _projectPath = projectPath;
    }

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
        if (!StShared.RunProcess(_useConsole, null, "git", $"-C {_projectPath} remote update"))
        {
            StShared.WriteErrorLine($"cannot run remote update for folder {_projectPath}", _useConsole, _logger);
            return GitState.Unknown;
        }

        var local = StShared.RunProcessWithOutput(_useConsole, null, "git", $"-C {_projectPath} rev-parse @");
        var remote = StShared.RunProcessWithOutput(_useConsole, null, "git", $"-C {_projectPath} rev-parse @{{u}}");
        var strBase = StShared.RunProcessWithOutput(_useConsole, null, "git", $"-C {_projectPath} merge-base @ @{{u}}");

        if (local == remote)
        {
            Console.WriteLine($"{_projectPath} Up to date");
            return GitState.UpToDate;
        }

        if (local == strBase)
        {
            Console.WriteLine("need to pull");
            return GitState.NeedToPull;
        }

        if (remote == strBase)
        {
            Console.WriteLine("need to push");
            return GitState.NeedToPush;
        }

        StShared.WriteErrorLine("Diverged", _useConsole, _logger);
        return GitState.Diverged;
    }


    //public bool IsGitRemoteAddressValid(string remoteAddress)
    //{
    //    return StShared.RunProcess(_useConsole, _logger, "git", $"ls-remote {remoteAddress}");
    //}


    public bool Pull()
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} pull"))
            return true;

        StShared.WriteErrorLine("cannot pull", _useConsole, _logger);
        return false;
    }

    public string GetRemoteOriginUrl()
    {
        return StShared
            .RunProcessWithOutput(_useConsole, null, "git", $"-C {_projectPath} config --get remote.origin.url")
            .Trim(Environment.NewLine.ToCharArray());
    }

    public bool Commit(string commitMessage)
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} commit -m \"{commitMessage}\""))
            return true;
        StShared.WriteErrorLine($"cannot run commit for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool NeedCommit()
    {
        var gitStatusOutput =
            StShared.RunProcessWithOutput(_useConsole, null, "git", $"-C {_projectPath} status --porcelain");
        return gitStatusOutput != "";
    }

    public bool Add()
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} add ."))
            return true;
        StShared.WriteErrorLine($"cannot run add for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Reset()
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} reset"))
            return true;
        StShared.WriteErrorLine($"cannot run reset for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Checkout()
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} checkout ."))
            return true;
        StShared.WriteErrorLine($"cannot run checkout for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Clean_fdx()
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} clean -fdx"))
            return true;
        StShared.WriteErrorLine($"cannot run clean -fdx for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool HaveUnTrackedFiles()
    {
        //return !StShared.RunProcess(_useConsole, null, "git", $"-C {_projectPath} diff-files --quiet", false);
        var statusCommandOutput = StShared.RunProcessWithOutput(_useConsole, null, "git", $"-C {_projectPath} status --porcelain --untracked-files");
        return !string.IsNullOrWhiteSpace(statusCommandOutput);
    }

    public bool IsGitInitialized()
    {
        return StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} rev-parse");
    }

    private bool Push()
    {
        if (StShared.RunProcess(_useConsole, _logger, "git", $"-C {_projectPath} push"))
            return true;

        StShared.WriteErrorLine("cannot push", _useConsole, _logger);
        return false;
    }

    public bool Clone(string remoteAddress)
    {
        //git clone git@bitbucket.org:mzakalashvili/systemtools.git SystemTools
        if (StShared.RunProcess(_useConsole, _logger, "git",
                $"clone {remoteAddress} {_projectPath}"))
            return true;
        StShared.WriteErrorLine($"cannot clone {remoteAddress} to {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool SyncRemote()
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

        while (true)
            switch (GetGitState())
            {
                case GitState.UpToDate:
                    return true;
                case GitState.NeedToPull:
                    if (!Pull())
                        return false;
                    break;
                case GitState.NeedToPush:
                    if (!Push())
                        return false;
                    break;
                case GitState.Diverged:
                    StShared.WriteErrorLine($"{_projectPath} Diverged", _useConsole, _logger);
                    return false;
                case GitState.Unknown:
                    StShared.WriteErrorLine($"{_projectPath} Unknown state", _useConsole, _logger);
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    }
}