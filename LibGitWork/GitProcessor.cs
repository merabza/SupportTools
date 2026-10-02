using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;
using SystemTools.SystemToolsShared;

namespace LibGitWork;

public sealed class GitProcessor
{
    private const string Git = "git";
    private readonly string _git;
    private readonly ILogger? _logger;
    private readonly string _projectPath;
    private readonly string _switchToProjectPath;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProcessor(bool useConsole, ILogger? logger, string projectPath, string? gitExecutablePath = null)
    {
        _useConsole = useConsole;
        _logger = logger;
        _projectPath = projectPath;
        _switchToProjectPath = $"-C {QuoteArgument(_projectPath)}";
        _git = string.IsNullOrWhiteSpace(gitExecutablePath) ? Git : gitExecutablePath;
    }

    public string? LastRemoteId { get; private set; }

    public void CheckRemoteId()
    {
        LastRemoteId = GitGetRemoteId();
    }

    //public OneOf<bool, Error[]> NeedPull(bool updateRemote = false)
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

        string? local = GitGetLocalId();
        if (local is null)
        {
            return GitState.Unknown;
        }

        LastRemoteId = GitGetRemoteId();
        if (LastRemoteId is null)
        {
            return GitState.Unknown;
        }

        string? strBase = GitGetBaseId();
        if (strBase is null)
        {
            return GitState.Unknown;
        }

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
        return GitState.Diverged;
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
        Result<(string, int)> localResult =
            StShared.RunProcessWithOutput(false, null, _git, $"{_switchToProjectPath} {parameters}");
        if (localResult.IsSuccess)
        {
            return localResult.Value.Item1;
        }

        StShared.WriteErrorLine($"{_git} {parameters} Error", _useConsole, _logger);
        return null;
    }

    public bool GitRemoteUpdate()
    {
        if (StShared.RunProcess(false, _logger, _git, $"{_switchToProjectPath} remote update").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot run remote update for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Pull()
    {
        if (StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} pull").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine("cannot pull", _useConsole, _logger);
        return false;
    }

    public Result<string> GetRemoteOriginUrl()
    {
        Result<(string, int)> result = StShared.RunProcessWithOutput(false, null, _git,
            $"{_switchToProjectPath} config --get remote.origin.url");
        if (result.IsFailure)
        {
            return result.Error;
        }

        return result.Value.Item1.Trim(Environment.NewLine.ToCharArray());
    }

    public bool Commit(string commitMessage)
    {
        if (StShared.RunProcess(_useConsole, _logger, _git,
                $"{_switchToProjectPath} commit -m {QuoteArgument(commitMessage)}").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot run commit for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public Result<bool> NeedCommit()
    {
        Result<(string, int)> gitStatusOutputResult =
            StShared.RunProcessWithOutput(false, null, _git, $"{_switchToProjectPath} status --porcelain");
        if (gitStatusOutputResult.IsFailure)
        {
            return gitStatusOutputResult.Error;
        }

        string gitStatusOutput = gitStatusOutputResult.Value.Item1;
        return !string.IsNullOrEmpty(gitStatusOutput);
    }

    public bool Add()
    {
        if (StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} add .").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot run add for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Reset()
    {
        if (StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} reset").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot run reset for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Checkout()
    {
        if (StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} checkout .").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot run checkout for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public bool Clean_fdx()
    {
        if (StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} clean -fdx").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot run clean -fdx for folder {_projectPath}", _useConsole, _logger);
        return false;
    }

    public Result<bool> HaveUnTrackedFiles()
    {
        //return !StShared.RunProcess(_useConsole, null, Git, $"{_switchToProjectPath} diff-files --quiet", false);
        Result<(string, int)> statusCommandOutputResult = StShared.RunProcessWithOutput(false, null, _git,
            $"{_switchToProjectPath} status --porcelain --untracked-files");

        if (statusCommandOutputResult.IsFailure)
        {
            return statusCommandOutputResult.Error;
        }

        string statusCommandOutput = statusCommandOutputResult.Value.Item1;

        return !string.IsNullOrWhiteSpace(statusCommandOutput);
    }

    public bool IsGitInitialized()
    {
        return StShared.RunProcess(false, _logger, _git, $"{_switchToProjectPath} rev-parse").IsSuccess;
    }

    private bool Push()
    {
        if (StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} push").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine("cannot push", _useConsole, _logger);
        return false;
    }

    public bool Clone(string remoteAddress)
    {
        if (GitCommandRunner.Run(CreateCloneStartInfo(remoteAddress), _useConsole, _logger))
        {
            CheckRemoteId();
            return true;
        }

        StShared.WriteErrorLine($"cannot clone {remoteAddress} to {_projectPath}", _useConsole, _logger);
        return false;
    }

    //"--"-ის შემდეგ "-"-ით დაწყებული მისამართიც (მაგალითად --upload-pack=...) git-ის ოფციად აღარ წაიკითხება
    internal ProcessStartInfo CreateCloneStartInfo(string remoteAddress)
    {
        return GitCommandRunner.CreateStartInfo(_git, "clone", "--", remoteAddress, _projectPath);
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
        bool pushed = false;

        if (LastRemoteId is null && !GitRemoteUpdate())
        {
            return (false, pushed);
        }

        while (true)
        {
            GitState gitState = GetGitState();
            switch (gitState)
            {
                case GitState.UpToDate:
                    return (true, pushed);
                case GitState.NeedToPull:
                case GitState.Diverged:
                    //Diverged-ის დროს pull აერთიანებს ლოკალურ და სერვერის ცვლილებებს
                    //(pull.rebase პარამეტრის მიხედვით merge-ით ან rebase-ით),
                    //შემდეგ კი ციკლის მომდევნო ბიჯი push-ს გააკეთებს
                    if (!Pull())
                    {
                        return (false, pushed);
                    }

                    break;
                case GitState.NeedToPush:
                    if (!Push())
                    {
                        return (false, pushed);
                    }

                    pushed = true;
                    break;
                case GitState.Unknown:
                    StShared.WriteErrorLine($"{_projectPath} Unknown state", _useConsole, _logger);
                    return (false, pushed);
                default:
                    throw new SwitchExpressionException($"Unexpected git state: {gitState}");
            }
        }
    }

    //ამოვკრიფოთ ყველა ფაილის სახელი, რომელიც .gitignore ფაილის მიხედვით არ ეკუთვნის ქეშირებას
    //git -C {GitPatch} ls-files -i --exclude-from=.gitignore -c
    //core.quotePath=true-ით git ყველა არა-ASCII ბაიტს რვაობითი კოდით ბეჭდავს, ამიტომ გამოტანა კონსოლის
    //კოდირებაზე არ არის დამოკიდებული, ნამდვილ სახელს კი UnquoteGitPath აღადგენს
    public Result<string[]> GetRedundantCachedFilesList()
    {
        //return !StShared.RunProcess(_useConsole, null, Git, $"{_switchToProjectPath} diff-files --quiet", false);
        Result<(string, int)> statusCommandOutputResult = StShared.RunProcessWithOutput(false, null, _git,
            $"{_switchToProjectPath} -c core.quotePath=true ls-files -i --exclude-from=.gitignore -c");

        if (statusCommandOutputResult.IsFailure)
        {
            return statusCommandOutputResult.Error;
        }

        string statusCommandOutput = statusCommandOutputResult.Value.Item1;

        string[] fileNames = string.IsNullOrWhiteSpace(statusCommandOutput)
            ? []
            : [.. statusCommandOutput.Split(Environment.NewLine).Select(UnquoteGitPath)];
        return fileNames;
    }

    //git-ის მიერ C სტილში დაბრჭყალებული ფაილის სახელის აღდგენა. მაგალითად, "\341\203\244.log" არის ფ.log:
    //\ooo რვაობითი ბაიტია, \a \b \t \n \v \f \r მართვის სიმბოლოებია, \" და \\ კი თავად ეს სიმბოლოები.
    //ბაიტებიდან სახელი UTF-8-ით აიწყობა. ბრჭყალების გარეშე დაბეჭდილი სახელი უცვლელი რჩება
    internal static string UnquoteGitPath(string path)
    {
        if (path.Length < 2 || path[0] != '"' || path[^1] != '"')
        {
            return path;
        }

        List<byte> bytes = [];
        int i = 1;
        while (i < path.Length - 1)
        {
            char c = path[i];
            if (c != '\\')
            {
                bytes.Add((byte)c);
                i++;
                continue;
            }

            char escaped = path[i + 1];
            if (escaped is >= '0' and <= '7')
            {
                bytes.Add(Convert.ToByte(path[(i + 1)..(i + 4)], 8));
                i += 4;
                continue;
            }

            byte value = escaped switch
            {
                'a' => 7,
                'b' => 8,
                't' => 9,
                'n' => 10,
                'v' => 11,
                'f' => 12,
                'r' => 13,
                _ => (byte)escaped
            };
            bytes.Add(value);
            i += 2;
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }

    //წავშალოთ ქეშიდან თითოეული ფაილისათვის შემდეგი ბრძანების გაშვებით
    //git -C {GitPatch} rm --cached -- {წინა ბრძანების მიერ დაბრუნებული ფაილის სახელი სრულად,
    //ანუ GitPatch-დან დაწყებული}
    //"--"-ის შემდეგ "-"-ით დაწყებული ფაილის სახელიც git-ის ოფციად აღარ წაიკითხება
    public bool RemoveFromCacheRedundantCachedFile(string redundantCachedFileName)
    {
        if (StShared.RunProcess(_useConsole, _logger, _git,
                $"{_switchToProjectPath} rm --cached -- {QuoteArgument(redundantCachedFileName)}").IsSuccess)
        {
            return true;
        }

        StShared.WriteErrorLine($"cannot remove file {redundantCachedFileName} from cache", _useConsole, _logger);
        return false;
    }

    public Result Initialise()
    {
        return StShared.RunProcess(_useConsole, _logger, _git, $"{_switchToProjectPath} init");
    }

    public bool IsFolderPartOfGitWorkingTree(string appFolderForDiffFullName)
    {
        Result<(string, int)> isInsideWorkTreeResult = StShared.RunProcessWithOutput(false, _logger, _git,
            $"-C {QuoteArgument(appFolderForDiffFullName)} rev-parse --is-inside-work-tree", [128]);
        if (isInsideWorkTreeResult.IsFailure)
        {
            return false;
        }

        (string, int) isInsideWorkTree = isInsideWorkTreeResult.Value;

        return isInsideWorkTree.Item2 == 0 && isInsideWorkTree.Item1 == "true" + Environment.NewLine;
    }

    /*
        var isInsideWorkTreeResult = StShared.RunProcessWithOutput(false, _logger, "git",
           $"-C \"{appFolderForDiffFullName}\" rev-parse --is-inside-work-tree", [128]);
     */

    //არგუმენტის ბრჭყალებში ჩასმა ბრძანების ხაზის სტანდარტული წესებით (CommandLineToArgvW), რომ ჰარების ან
    //ბრჭყალების შემცველი გზა თუ მესიჯი git-მა ერთ, უცვლელ არგუმენტად მიიღოს: ბრჭყალის და დამხურავი
    //ბრჭყალის წინ მდგომი უკუხაზები ორმაგდება, თვითონ ბრჭყალი კი \"-ად იწერება
    internal static string QuoteArgument(string argument)
    {
        var sb = new StringBuilder();
        sb.Append('"');
        int backslashCount = 0;
        foreach (char c in argument)
        {
            if (c == '\\')
            {
                backslashCount++;
                continue;
            }

            sb.Append('\\', c == '"' ? backslashCount * 2 + 1 : backslashCount);
            sb.Append(c);
            backslashCount = 0;
        }

        sb.Append('\\', backslashCount * 2);
        sb.Append('"');
        return sb.ToString();
    }
}
