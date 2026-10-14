using LibGitWork.SyncV3;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.SyncV3;

public sealed class GitSyncV3ErrorsTests
{
    [Fact]
    public void GitCommandFailed_WhenGitWroteAnError_DescribesTheCommandTheExitCodeAndTheError()
    {
        // Act
        Error error = GitSyncV3Errors.GitCommandFailed("git push", 1, "! [rejected] main -> main (fetch first)");

        // Assert
        Assert.Equal("GitCommandFailed", error.Code);
        Assert.Equal("git push failed (ExitCode=1): ! [rejected] main -> main (fetch first)", error.Description);
        Assert.Equal(ErrorType.Problem, error.Type);
    }

    [Fact]
    public void GitCommandFailed_WhenGitWroteNoError_DescribesOnlyTheCommandAndTheExitCode()
    {
        // Act
        Error error = GitSyncV3Errors.GitCommandFailed("git status", 128, string.Empty);

        // Assert
        Assert.Equal("git status failed (ExitCode=128)", error.Description);
    }
}
