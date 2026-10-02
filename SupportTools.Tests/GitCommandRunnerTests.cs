using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using LibGitWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SupportTools.Tests;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitCommandRunnerTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public GitCommandRunnerTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        foreach (string filePath in Directory.EnumerateFiles(_tempFolder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        Directory.Delete(_tempFolder, true);
    }

    //every value stays one argument, even an empty one or one with spaces; nothing goes to the command line string
    [Fact]
    public void CreateStartInfo_WhenCalled_KeepsEveryArgumentWhole()
    {
        // Act
        ProcessStartInfo startInfo =
            GitCommandRunner.CreateStartInfo("git", "commit", "-m", "two words", string.Empty, "--", "-dash");

        // Assert
        Assert.Equal("git", startInfo.FileName);
        Assert.Equal(["commit", "-m", "two words", string.Empty, "--", "-dash"], startInfo.ArgumentList);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public void CreateStartInfo_WhenThereAreNoArguments_LeavesTheListEmpty()
    {
        // Act
        ProcessStartInfo startInfo = GitCommandRunner.CreateStartInfo("C:\\Tools\\Git\\git.exe");

        // Assert
        Assert.Equal("C:\\Tools\\Git\\git.exe", startInfo.FileName);
        Assert.Empty(startInfo.ArgumentList);
    }

    [Fact]
    public void Run_WhenGitSucceeds_LogsTheCommandAndReturnsTrue()
    {
        // Arrange
        string repository = Path.Combine(_tempFolder, "with space", "Repository");
        ProcessStartInfo startInfo = GitCommandRunner.CreateStartInfo("git", "init", "-q", repository);

        // Act
        bool result = GitCommandRunner.Run(startInfo, false, _logger.Object);

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(repository, ".git")));
        VerifyLogged(LogLevel.Information, $"Running git init -q {repository}...");
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void Run_WhenConsoleIsUsed_WritesTheCommandToTheConsole()
    {
        // Arrange
        string repository = Path.Combine(_tempFolder, "Repository");
        ProcessStartInfo startInfo = GitCommandRunner.CreateStartInfo("git", "init", "-q", repository);

        // Act
        bool result = GitCommandRunner.Run(startInfo, true, null);

        // Assert
        Assert.True(result);
        Assert.Contains($"Running git init -q {repository}...", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenGitFails_LogsTheExitCodeAndReturnsFalse()
    {
        // Arrange
        string missingRepository = Path.Combine(_tempFolder, "missing.git");
        ProcessStartInfo startInfo = GitCommandRunner.CreateStartInfo("git", "ls-remote", "--", missingRepository);

        // Act
        bool result = GitCommandRunner.Run(startInfo, false, _logger.Object);

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error,
            $"git ls-remote -- {missingRepository} process was finished with errors. ExitCode=128");
    }

    private void VerifyLogged(LogLevel logLevel, string message)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(logLevel, It.IsAny<EventId>(), It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }
}
