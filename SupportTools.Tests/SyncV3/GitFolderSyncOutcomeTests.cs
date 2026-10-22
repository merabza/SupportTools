using LibGitWork.SyncV3;
using Xunit;

namespace SupportTools.Tests.SyncV3;

public sealed class GitFolderSyncOutcomeTests
{
    private static readonly GitSyncFolder Folder = new("RepoA", "git@github.com:test/RepoA.git", "AppA",
        @"D:\AppA\RepoA");

    //the error of the local check is the error of the folder, so the sync leaves the folder alone
    [Fact]
    public void Constructor_WhenTheCheckFailed_TakesItsError()
    {
        // Arrange
        var inspection = new GitFolderInspection(Folder, EGitFolderInspectionKind.Failed, null, "is not a repository");

        // Act
        var sut = new GitFolderSyncOutcome(inspection);

        // Assert
        Assert.Same(inspection, sut.Inspection);
        Assert.Same(Folder, sut.Folder);
        Assert.Equal("is not a repository", sut.Error);
        Assert.False(sut.WithoutRequest);
    }

    [Fact]
    public void Constructor_WhenTheFolderIsReady_HasNoErrorAndNoRequestYet()
    {
        // Act
        var sut = new GitFolderSyncOutcome(new GitFolderInspection(Folder, EGitFolderInspectionKind.Ready,
            new GitFolderStatus("abc", "main", false, false), null));

        // Assert
        Assert.Null(sut.Error);
        Assert.True(sut.WithoutRequest);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void WithoutRequest_WhenTheFolderWasClonedFetchedOrFailed_IsFalse(bool cloned, bool fetched, bool failed)
    {
        // Arrange
        var sut = new GitFolderSyncOutcome(new GitFolderInspection(Folder, EGitFolderInspectionKind.Ready,
            new GitFolderStatus("abc", "main", false, false), null))
        {
            Cloned = cloned, Fetched = fetched, Error = failed ? "push failed" : null
        };

        // Act
        bool withoutRequest = sut.WithoutRequest;

        // Assert
        Assert.False(withoutRequest);
    }

    //a commit, a merge or a push alone do not need the git host here; the fetch before them does
    [Fact]
    public void WithoutRequest_WhenOnlyTheLocalFlagsAreSet_IsTrue()
    {
        // Arrange
        var sut = new GitFolderSyncOutcome(new GitFolderInspection(Folder, EGitFolderInspectionKind.Ready,
            new GitFolderStatus("abc", "main", false, false), null)) { Committed = true, Pulled = true, Pushed = true };

        // Act
        bool withoutRequest = sut.WithoutRequest;

        // Assert
        Assert.True(withoutRequest);
    }
}
