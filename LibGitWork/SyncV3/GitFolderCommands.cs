using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SystemTools.SharedKernel;

namespace LibGitWork.SyncV3;

//V3 სინქრონიზაციის git ბრძანებები. git-ის გამოტანა კონსოლზე არ გადის (პარალელური სამუშაოების დაფას არ არღვევს), ის და
//შეცდომის ტექსტი შედეგში ბრუნდება. არგუმენტები ArgumentList-ით გადაეცემა, ამიტომ ჰარის შემცველი გზა ან მესიჯი ერთ
//არგუმენტად რჩება. git არაფერს ეკითხება მომხმარებელს: GIT_TERMINAL_PROMPT=0 (პაროლის კითხვის ნაცვლად შეცდომა),
//GIT_MERGE_AUTOEDIT=no (merge-ის მესიჯი რედაქტორის გარეშე) და დახურული სტანდარტული შესასვლელი
internal sealed class GitFolderCommands
{
    private const string Git = "git";
    private readonly string _git;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitFolderCommands(string? gitExecutablePath)
    {
        _git = string.IsNullOrWhiteSpace(gitExecutablePath) ? Git : gitExecutablePath;
    }

    public Result<string> GetOriginUrl(string folder)
    {
        Result<string> result = Run(folder, "config", "--get", "remote.origin.url");
        return result.IsFailure ? result.Error : result.Value.Trim();
    }

    //ფაილები, რომლებიც .gitignore-ის მიხედვით ქეშში არ უნდა იყოს. core.quotePath=true-ით არა-ASCII სახელი რვაობითი
    //კოდებით იბეჭდება, ნამდვილ სახელს კი GitProcessor.UnquoteGitPath აღადგენს
    public Result<string[]> GetRedundantCachedFiles(string folder)
    {
        Result<string> result = Run(folder, "-c", "core.quotePath=true", "ls-files", "-i", "--exclude-from=.gitignore",
            "-c");
        if (result.IsFailure)
        {
            return result.Error;
        }

        return result.Value.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(GitProcessor.UnquoteGitPath).ToArray();
    }

    public Result RemoveFromCache(string folder, string fileName)
    {
        return ToResult(Run(folder, "rm", "--cached", "--", fileName));
    }

    public Result<GitFolderStatus> ReadStatus(string folder)
    {
        Result<string> result = Run(folder, "status", "--porcelain=v2", "--branch", "--untracked-files=normal");
        return result.IsFailure ? result.Error : GitFolderStatus.Parse(result.Value);
    }

    public Result AddAll(string folder)
    {
        return ToResult(Run(folder, "add", "."));
    }

    public Result Commit(string folder, string message)
    {
        return ToResult(Run(folder, "commit", "-m", message));
    }

    public Result RemoteUpdate(string folder)
    {
        return ToResult(Run(folder, "remote", "update"));
    }

    public Result<string> RevParse(string folder, string revision)
    {
        Result<string> result = Run(folder, "rev-parse", revision);
        return result.IsFailure ? result.Error : result.Value.Trim();
    }

    public Result<string> MergeBase(string folder)
    {
        Result<string> result = Run(folder, "merge-base", "@", "@{u}");
        return result.IsFailure ? result.Error : result.Value.Trim();
    }

    //ჩამორჩენილი ფოლდერი fetch-ის შემდეგ ქსელის გარეშე ეწევა upstream-ს (pull ხელახლა გააკეთებდა fetch-ს)
    public Result MergeFastForward(string folder)
    {
        return ToResult(Run(folder, "merge", "--ff-only", "@{u}"));
    }

    //დაშორებული ისტორიები pull-ით ერთიანდება, pull.rebase პარამეტრის მიხედვით merge-ით ან rebase-ით
    public Result Pull(string folder)
    {
        return ToResult(Run(folder, "pull"));
    }

    public Result Push(string folder)
    {
        return ToResult(Run(folder, "push"));
    }

    //remote-ის ცნობილი ვერსია ლოკალურ refs/remotes/origin/<branch>-ში იწერება ისე, როგორც ამას fetch გააკეთებდა
    public Result UpdateTrackingRef(string folder, string branchName, string commitId)
    {
        return ToResult(Run(folder, "update-ref", $"refs/remotes/origin/{branchName}", commitId));
    }

    //"--"-ის შემდეგ "-"-ით დაწყებული მისამართიც რეპოზიტორიად აღიქმება და არა ოფციად. git საჭირო ფოლდერებს თვითონ ქმნის
    public Result Clone(string address, string folder)
    {
        return ToResult(Run(null, "clone", "--", address, folder));
    }

    internal ProcessStartInfo CreateStartInfo(string? folder, string[] arguments)
    {
        var startInfo = new ProcessStartInfo(_git)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };
        if (folder is not null)
        {
            startInfo.ArgumentList.Add("-C");
            startInfo.ArgumentList.Add(folder);
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GIT_MERGE_AUTOEDIT"] = "no";
        return startInfo;
    }

    private Result<string> Run(string? folder, params string[] arguments)
    {
        using var process = new Process();
        process.StartInfo = CreateStartInfo(folder, arguments);
        var errorOutput = new StringBuilder();
        //stderr ასინქრონულად იკითხება, რომ სავსე pipe-ზე პროცესი არ გაიჭედოს
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errorOutput.AppendLine(e.Data);
            }
        };
        process.Start();
        process.StandardInput.Close();
        process.BeginErrorReadLine();
        string output = process.StandardOutput.ReadToEnd();
        //უპარამეტრო WaitForExit stderr-ის ასინქრონული კითხვის დასრულებასაც ელოდება
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return output;
        }

        return GitSyncV3Errors.GitCommandFailed($"git {string.Join(' ', arguments)}", process.ExitCode,
            errorOutput.ToString().Trim());
    }

    private static Result ToResult(Result<string> result)
    {
        return result.IsFailure ? result.Error : Result.Success();
    }
}
