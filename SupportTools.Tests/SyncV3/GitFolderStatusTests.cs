using LibGitWork.SyncV3;
using Xunit;

namespace SupportTools.Tests.SyncV3;

public sealed class GitFolderStatusTests
{
    private const string Commit = "1dfa466db703de783bc9929e9bd765f2c870a1f2";

    [Fact]
    public void Parse_WhenTheFolderIsCleanAndAtItsUpstream_ReadsTheHeadAndTheBranch()
    {
        // Act
        var status = GitFolderStatus.Parse(
            $"# branch.oid {Commit}\n# branch.head main\n# branch.upstream origin/main\n# branch.ab +0 -0\n");

        // Assert
        Assert.Equal(new GitFolderStatus(Commit, "main", false, false), status);
    }

    //git on Windows ends the lines with \r\n
    [Fact]
    public void Parse_WhenThereAreChanges_ReportsThem()
    {
        // Act
        var status = GitFolderStatus.Parse(
            $"# branch.oid {Commit}\r\n# branch.head main\r\n# branch.upstream origin/main\r\n# branch.ab +1 -0\r\n" +
            "1 .M N... 100644 100644 100644 abc abc Readme.txt\r\n? New file.txt\r\n");

        // Assert
        Assert.Equal(new GitFolderStatus(Commit, "main", true, true), status);
    }

    //a branch name may hold slashes; only the name of the remote is cut off
    [Fact]
    public void Parse_WhenTheBranchNameHasSlashes_KeepsThem()
    {
        // Act
        var status = GitFolderStatus.Parse(
            $"# branch.oid {Commit}\n# branch.head feature/x\n# branch.upstream origin/feature/x\n# branch.ab +0 -2\n");

        // Assert
        Assert.Equal("feature/x", status.UpstreamBranch);
        Assert.True(status.TrackingRefDiffers);
    }

    //only an upstream of origin can be compared with the head that SupportToolsServer knows
    [Fact]
    public void Parse_WhenTheUpstreamIsOfAnotherRemote_HasNoUpstreamBranch()
    {
        // Act
        var status = GitFolderStatus.Parse(
            $"# branch.oid {Commit}\n# branch.head main\n# branch.upstream fork/main\n# branch.ab +0 -0\n");

        // Assert
        Assert.Null(status.UpstreamBranch);
    }

    [Fact]
    public void Parse_WhenTheHeadIsDetached_HasNoUpstreamBranch()
    {
        // Act
        var status = GitFolderStatus.Parse($"# branch.oid {Commit}\n# branch.head (detached)\n");

        // Assert
        Assert.Equal(new GitFolderStatus(Commit, null, true, false), status);
    }

    //a repository without commits, or an upstream whose tracking ref is missing (no branch.ab line)
    [Fact]
    public void Parse_WhenThereIsNoCommitYet_HasNoHeadCommit()
    {
        // Act
        var status = GitFolderStatus.Parse("# branch.oid (initial)\n# branch.head main\n# branch.upstream origin/main\n");

        // Assert
        Assert.Equal(new GitFolderStatus(null, "main", true, false), status);
    }
}
