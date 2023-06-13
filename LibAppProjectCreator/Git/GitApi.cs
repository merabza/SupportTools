using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibAppProjectCreator.Git;

public sealed class GitApi
{
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public GitApi(bool useConsole, ILogger logger)
    {
        _useConsole = useConsole;
        _logger = logger;
    }

    public bool IsGitRemoteAddressValid(string remoteAddress)
    {
        return StShared.RunProcess(_useConsole, _logger, "git", $"ls-remote {remoteAddress}");
    }
}