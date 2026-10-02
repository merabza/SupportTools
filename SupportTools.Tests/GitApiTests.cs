using System;
using System.Diagnostics;
using System.IO;
using LibGitWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SupportTools.Tests;

public sealed class GitApiTests : IDisposable
{
    private readonly Mock<ILogger> _logger = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    //the address is a single argument after the end of the options: one starting with "-" is not read as an option
    [Theory]
    [InlineData("git@github.com:x/a.git")]
    [InlineData("--upload-pack=touch pwned")]
    [InlineData("-oProxyCommand=calc")]
    [InlineData("C:\\Repos\\with space\\a.git")]
    public void CreateLsRemoteStartInfo_WhenCalled_PassesTheAddressAsOneArgumentAfterTheEndOfOptions(
        string remoteAddress)
    {
        // Act
        ProcessStartInfo startInfo = new GitApi(false, _logger.Object).CreateLsRemoteStartInfo(remoteAddress);

        // Assert
        Assert.Equal("git", startInfo.FileName);
        Assert.Equal(["ls-remote", "--", remoteAddress], startInfo.ArgumentList);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void CreateLsRemoteStartInfo_WhenGitPathIsGiven_RunsThatGit()
    {
        // Arrange
        string gitExecutablePath = Path.Combine(_tempFolder, "git.exe");

        // Act
        ProcessStartInfo startInfo =
            new GitApi(false, _logger.Object, gitExecutablePath).CreateLsRemoteStartInfo("git@github.com:x/a.git");

        // Assert
        Assert.Equal(gitExecutablePath, startInfo.FileName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateLsRemoteStartInfo_WhenGitPathIsEmpty_RunsGitFromThePath(string? gitExecutablePath)
    {
        // Act
        ProcessStartInfo startInfo =
            new GitApi(false, _logger.Object, gitExecutablePath).CreateLsRemoteStartInfo("git@github.com:x/a.git");

        // Assert
        Assert.Equal("git", startInfo.FileName);
    }

    //inserted into a command line string, such an address reached git as two arguments
    [Fact]
    public void IsGitRemoteAddressValid_WhenRepositoryPathContainsASpace_ReturnsTrue()
    {
        // Arrange
        string repository = Path.Combine(_tempFolder, "with space", "repository.git");
        InitBareRepository(repository);

        // Act
        bool result = new GitApi(false, _logger.Object).IsGitRemoteAddressValid(repository);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsGitRemoteAddressValid_WhenAddressIsNotARepository_LogsTheErrorAndReturnsFalse()
    {
        // Arrange
        string missingRepository = Path.Combine(_tempFolder, "missing.git");

        // Act
        bool result = new GitApi(false, _logger.Object).IsGitRemoteAddressValid(missingRepository);

        // Assert
        Assert.False(result);
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("process was finished with errors", StringComparison.Ordinal)),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private static void InitBareRepository(string repository)
    {
        using Process git = Process.Start(new ProcessStartInfo("git")
        {
            ArgumentList = { "init", "--bare", repository },
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);
    }
}
