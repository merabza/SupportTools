using Microsoft.Extensions.Logging;

namespace SupportTools.Tests;

//one command of DotnetProcessor as FakeDotnetRunner saw it. WaitForExit is null for a command with output
//(RunProcessWithOutput has no such parameter)
internal sealed record FakeDotnetRun(
    bool UseConsole,
    ILogger? Logger,
    string ProgramFileName,
    string Arguments,
    int[]? AllowExitCodes,
    bool UseErrorLine,
    int? WaitForExit);
