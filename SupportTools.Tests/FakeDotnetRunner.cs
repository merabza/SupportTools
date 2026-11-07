using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SystemTools.SharedKernel;

namespace SupportTools.Tests;

//runs no process: records every command of DotnetProcessor with its flags and answers with Result (a command without
//output) or Output (a command with output); OnRun runs with the arguments before the answer, e.g. to write a build log
internal sealed class FakeDotnetRunner
{
    public List<FakeDotnetRun> Runs { get; } = [];
    public Result Result { get; set; } = Result.Success();
    public Result<(string, int)> Output { get; set; } = Result.Success((string.Empty, 0));
    public Action<string>? OnRun { get; set; }

    public Result RunProcess(bool useConsole, ILogger? logger, string programFileName, string arguments,
        int[]? allowExitCodes, bool useErrorLine, int waitForExit)
    {
        Record(new FakeDotnetRun(useConsole, logger, programFileName, arguments, allowExitCodes, useErrorLine,
            waitForExit));
        return Result;
    }

    public Result<(string, int)> RunProcessWithOutput(bool useConsole, ILogger? logger, string programFileName,
        string arguments, int[]? allowExitCodes, bool useErrorLine)
    {
        Record(new FakeDotnetRun(useConsole, logger, programFileName, arguments, allowExitCodes, useErrorLine, null));
        return Output;
    }

    private void Record(FakeDotnetRun run)
    {
        Runs.Add(run);
        OnRun?.Invoke(run.Arguments);
    }
}

