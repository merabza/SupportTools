using System;
using System.Collections.Generic;
using System.IO;
using LibGitWork.OutdatedPackages;
using SystemTools.SharedKernel;

namespace SupportTools.Tests.OutdatedPackages;

//records the commands as text by file or folder name ("restore for outdated <solution>", "outdated <folder>",
//"restore after outdated <solution>"); OnOutdated runs at every dotnet outdated attempt (a test writes the versions
//there), and the first OutdatedFailures attempts fail
internal sealed class FakePackageUpdaterCommands : IPackageUpdaterCommands
{
    private static readonly Error FakeError = Error.Failure("FakeCommand", "the fake command failed");

    public List<string> Commands { get; } = [];
    public int OutdatedFailures { get; set; }
    public bool RestoreForOutdatedFails { get; set; }
    public bool RestoreAfterOutdatedFails { get; set; }
    public Action? OnOutdated { get; set; }

    public Result RestoreForOutdated(string solutionFileFullName)
    {
        Commands.Add($"restore for outdated {Path.GetFileName(solutionFileFullName)}");
        return RestoreForOutdatedFails ? Result.Failure(FakeError) : Result.Success();
    }

    public Result UpdateOutdatedPackages(string projectFolderName)
    {
        Commands.Add($"outdated {Path.GetFileName(projectFolderName)}");
        OnOutdated?.Invoke();
        if (OutdatedFailures == 0)
        {
            return Result.Success();
        }

        OutdatedFailures--;
        return Result.Failure(FakeError);
    }

    public Result RestoreAfterOutdated(string solutionFileFullName)
    {
        Commands.Add($"restore after outdated {Path.GetFileName(solutionFileFullName)}");
        return RestoreAfterOutdatedFails ? Result.Failure(FakeError) : Result.Success();
    }
}
