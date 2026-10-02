using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

namespace LibGitWork;

//git-ის გაშვება არგუმენტების სიით (ProcessStartInfo.ArgumentList): თითო მნიშვნელობა ერთ არგუმენტად რჩება და
//whitespace-ით ვერ გაიყოფა. გარედან მოსული მნიშვნელობა (მისამართი, ფაილის სახელი) "--"-ის შემდეგ უნდა იდგეს, რომ
//"-"-ით დაწყებული მნიშვნელობა git-მა ოფციად არ წაიკითხოს
internal static class GitCommandRunner
{
    public static ProcessStartInfo CreateStartInfo(string git, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(git) { UseShellExecute = false };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    public static bool Run(ProcessStartInfo startInfo, bool useConsole, ILogger? logger)
    {
        string commandLine = $"{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}";
        StShared.ConsoleWriteInformationLine(logger, useConsole, "Running {0}...", commandLine);

        using Process process = Process.Start(startInfo) ??
                                throw new InvalidOperationException($"{commandLine} process was not started");
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            return true;
        }

        StShared.WriteErrorLine($"{commandLine} process was finished with errors. ExitCode={process.ExitCode}",
            useConsole, logger);
        return false;
    }
}
