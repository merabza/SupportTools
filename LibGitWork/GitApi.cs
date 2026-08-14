using Microsoft.Extensions.Logging;
using SystemTools.SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibGitWork;

public sealed class GitApi
{
    private const string Git = "git";
    private readonly string _git;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public GitApi(bool useConsole, ILogger logger, string? gitExecutablePath = null)
    {
        _useConsole = useConsole;
        _logger = logger;
        _git = string.IsNullOrWhiteSpace(gitExecutablePath) ? Git : gitExecutablePath;
    }

    public bool IsGitRemoteAddressValid(string remoteAddress)
    {
        return StShared.RunProcess(_useConsole, _logger, _git, $"ls-remote {remoteAddress}").IsNone;
    }
}
