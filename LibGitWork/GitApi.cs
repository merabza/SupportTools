using System.Diagnostics;
using Microsoft.Extensions.Logging;

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
        return GitCommandRunner.Run(CreateLsRemoteStartInfo(remoteAddress), _useConsole, _logger);
    }

    //მისამართი ცალკე არგუმენტად გადაეცემა, ამიტომ whitespace-ით ვერ გაიყოფა. "--"-ის შემდეგ "-"-ით დაწყებული
    //მისამართიც (მაგალითად --upload-pack=...) git-ის ოფციად აღარ წაიკითხება
    internal ProcessStartInfo CreateLsRemoteStartInfo(string remoteAddress)
    {
        return GitCommandRunner.CreateStartInfo(_git, "ls-remote", "--", remoteAddress);
    }
}
