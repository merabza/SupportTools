using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LibGitWork.SyncV3;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.SyncV3;

//every git command of the V3 sync, with the real git over a throwaway bare remote (a local path, no network). The
//remote lies in a folder with a Georgian name, so the output and the error text must be read as UTF-8
public sealed class GitFolderCommandsTests : IDisposable
{
    private const string GeorgianFolderName = "ქართული";

    private readonly string _clone;
    private readonly string _remote;
    private readonly GitFolderCommands _sut = new(null);
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public GitFolderCommandsTests()
    {
        _remote = GitTestRepositories.CreateRemote(Path.Combine(_tempFolder, GeorgianFolderName), "RepoA",
            new Dictionary<string, string> { ["Readme.txt"] = "RepoA", [".gitignore"] = "bin/" });
        _clone = Clone("A");
    }

    public void Dispose()
    {
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public void GetOriginUrl_WhenTheFolderIsAClone_ReturnsTheAddressWithoutTheLineEnd()
    {
        // Act
        Result<string> result = _sut.GetOriginUrl(_clone);

        // Assert
        Assert.Equal(_remote, result.Value);
    }

    //the error text of git names the missing folder, in UTF-8
    [Fact]
    public void GetOriginUrl_WhenTheFolderIsMissing_FailsWithTheErrorOfGit()
    {
        // Arrange
        string folder = Path.Combine(_tempFolder, GeorgianFolderName, "Missing");

        // Act
        Result<string> result = _sut.GetOriginUrl(folder);

        // Assert
        Assert.Equal("GitCommandFailed", result.Error.Code);
        Assert.StartsWith("git config --get remote.origin.url failed (ExitCode=128): ", result.Error.Description,
            StringComparison.Ordinal);
        Assert.Contains(GeorgianFolderName, result.Error.Description, StringComparison.Ordinal);
    }

    //the names come back decoded from the octal escapes of git
    [Fact]
    public void GetRedundantCachedFiles_WhenIgnoredFilesAreCached_ReturnsTheirNames()
    {
        // Arrange
        CommitIgnoredFile("bin/ფ.dll");
        CommitIgnoredFile("bin/a b.dll");

        // Act
        Result<string[]> result = _sut.GetRedundantCachedFiles(_clone);

        // Assert
        Assert.Equal(["bin/a b.dll", "bin/ფ.dll"], result.Value);
    }

    [Fact]
    public void GetRedundantCachedFiles_WhenNothingIgnoredIsCached_ReturnsNoName()
    {
        // Act
        Result<string[]> result = _sut.GetRedundantCachedFiles(_clone);

        // Assert
        Assert.Empty(result.Value);
    }

    [Fact]
    public void GetRedundantCachedFiles_WhenThereIsNoGitignore_Fails()
    {
        // Arrange
        File.Delete(Path.Combine(_clone, ".gitignore"));

        // Act
        Result<string[]> result = _sut.GetRedundantCachedFiles(_clone);

        // Assert
        Assert.StartsWith("git -c core.quotePath=true ls-files -i --exclude-from=.gitignore -c failed",
            result.Error.Description, StringComparison.Ordinal);
    }

    //a name that starts with "-" is a file after "--", not an option of git rm; the file stays on the disk
    [Fact]
    public void RemoveFromCache_WhenTheFileIsCached_RemovesItOnlyFromTheIndex()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_clone, "-n.txt"), "n");
        GitTestRepositories.RunGit("-C", _clone, "add", "--", "-n.txt");
        GitTestRepositories.RunGit("-C", _clone, "commit", "-q", "-m", "Dash file");

        // Act
        Result result = _sut.RemoveFromCache(_clone, "-n.txt");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("D  -n.txt\n?? -n.txt",
            GitTestRepositories.RunGit("-C", _clone, "status", "--porcelain").Replace("\r", string.Empty,
                StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(_clone, "-n.txt")));
    }

    [Fact]
    public void RemoveFromCache_WhenTheFileIsNotCached_Fails()
    {
        // Act
        Result result = _sut.RemoveFromCache(_clone, "Missing.txt");

        // Assert
        Assert.StartsWith("git rm --cached -- Missing.txt failed", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadStatus_WhenTheFolderIsAClone_ReadsItsHeadAndUpstream()
    {
        // Act
        Result<GitFolderStatus> result = _sut.ReadStatus(_clone);

        // Assert
        Assert.Equal(new GitFolderStatus(Head(_clone), "main", false, false), result.Value);
    }

    [Fact]
    public void ReadStatus_WhenThereIsANewFile_HasChanges()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_clone, "New.txt"), "new");

        // Act
        Result<GitFolderStatus> result = _sut.ReadStatus(_clone);

        // Assert
        Assert.True(result.Value.HasChanges);
    }

    [Fact]
    public void ReadStatus_WhenTheFolderIsMissing_Fails()
    {
        // Act
        Result<GitFolderStatus> result = _sut.ReadStatus(Path.Combine(_tempFolder, "Missing"));

        // Assert
        Assert.StartsWith("git status --porcelain=v2 --branch --untracked-files=normal failed",
            result.Error.Description, StringComparison.Ordinal);
    }

    //the message keeps its spaces and quotes: it is one argument
    [Fact]
    public void AddAllAndCommit_WhenThereIsANewFile_CommitItWithTheMessage()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_clone, "New.txt"), "new");

        // Act
        Result addResult = _sut.AddAll(_clone);
        Result commitResult = _sut.Commit(_clone, "Fix \"a\" & b");

        // Assert
        Assert.True(addResult.IsSuccess);
        Assert.True(commitResult.IsSuccess);
        Assert.Equal("Fix \"a\" & b", GitTestRepositories.RunGit("-C", _clone, "log", "-1", "--format=%s"));
        Assert.Equal("New.txt", GitTestRepositories.RunGit("-C", _clone, "show", "--name-only", "--format="));
    }

    [Fact]
    public void AddAll_WhenTheFolderIsMissing_Fails()
    {
        // Act
        Result result = _sut.AddAll(Path.Combine(_tempFolder, "Missing"));

        // Assert
        Assert.StartsWith("git add . failed", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Commit_WhenNothingIsStaged_Fails()
    {
        // Act
        Result result = _sut.Commit(_clone, "Nothing");

        // Assert
        Assert.StartsWith("git commit -m Nothing failed (ExitCode=1)", result.Error.Description,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RemoteUpdate_WhenTheRemoteHasNewCommits_MovesTheTrackingRef()
    {
        // Arrange
        string newCommit = PushNewCommitFromAnotherClone("b.txt");

        // Act
        Result result = _sut.RemoteUpdate(_clone);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newCommit, GitTestRepositories.RunGit("-C", _clone, "rev-parse", "origin/main"));
    }

    [Fact]
    public void RemoteUpdate_WhenTheRemoteIsGone_Fails()
    {
        // Arrange
        GitTestRepositories.DeleteFolder(_remote);

        // Act
        Result result = _sut.RemoteUpdate(_clone);

        // Assert
        Assert.StartsWith("git remote update failed", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void RevParse_WhenTheRevisionExists_ReturnsTheCommitWithoutTheLineEnd()
    {
        // Act
        Result<string> result = _sut.RevParse(_clone, "@{u}");

        // Assert
        Assert.Equal(Head(_clone), result.Value);
    }

    [Fact]
    public void RevParse_WhenTheRevisionIsUnknown_Fails()
    {
        // Act
        Result<string> result = _sut.RevParse(_clone, "unknown-branch");

        // Assert
        Assert.StartsWith("git rev-parse unknown-branch failed", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void MergeBase_WhenTheCloneIsBehind_ReturnsItsHead()
    {
        // Arrange
        string head = Head(_clone);
        PushNewCommitFromAnotherClone("b.txt");
        GitTestRepositories.RunGit("-C", _clone, "fetch", "-q");

        // Act
        Result<string> result = _sut.MergeBase(_clone);

        // Assert
        Assert.Equal(head, result.Value);
    }

    [Fact]
    public void MergeBase_WhenTheHeadIsDetached_Fails()
    {
        // Arrange
        GitTestRepositories.RunGit("-C", _clone, "checkout", "-q", "--detach");

        // Act
        Result<string> result = _sut.MergeBase(_clone);

        // Assert
        Assert.StartsWith("git merge-base @ @{u} failed", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void MergeFastForward_WhenTheCloneIsBehind_MovesItToTheUpstream()
    {
        // Arrange
        string newCommit = PushNewCommitFromAnotherClone("b.txt");
        GitTestRepositories.RunGit("-C", _clone, "fetch", "-q");

        // Act
        Result result = _sut.MergeFastForward(_clone);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newCommit, Head(_clone));
    }

    //a fast-forward only merge never makes a merge commit
    [Fact]
    public void MergeFastForward_WhenTheHistoriesDiverged_Fails()
    {
        // Arrange
        PushNewCommitFromAnotherClone("b.txt");
        GitTestRepositories.RunGit("-C", _clone, "fetch", "-q");
        CommitFile(_clone, "a.txt");

        // Act
        Result result = _sut.MergeFastForward(_clone);

        // Assert
        Assert.StartsWith("git merge --ff-only @{u} failed", result.Error.Description, StringComparison.Ordinal);
    }

    //the clone is configured with pull.rebase=true: its own commit goes on top of the remote one
    [Fact]
    public void Pull_WhenTheHistoriesDiverged_PutsTheLocalCommitOnTopOfTheRemoteOne()
    {
        // Arrange
        string remoteCommit = PushNewCommitFromAnotherClone("b.txt");
        CommitFile(_clone, "a.txt");

        // Act
        Result result = _sut.Pull(_clone);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(remoteCommit, GitTestRepositories.RunGit("-C", _clone, "rev-parse", "HEAD~1"));
    }

    [Fact]
    public void Pull_WhenTheRemoteIsGone_Fails()
    {
        // Arrange
        GitTestRepositories.DeleteFolder(_remote);

        // Act
        Result result = _sut.Pull(_clone);

        // Assert
        Assert.StartsWith("git pull failed", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void Push_WhenTheCloneIsAhead_SendsItsCommit()
    {
        // Arrange
        string commit = CommitFile(_clone, "a.txt");

        // Act
        Result result = _sut.Push(_clone);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(commit, GitTestRepositories.RunGit("-C", _remote, "rev-parse", "main"));
    }

    [Fact]
    public void Push_WhenTheRemoteHasCommitsTheCloneLacks_IsRejected()
    {
        // Arrange
        PushNewCommitFromAnotherClone("b.txt");
        CommitFile(_clone, "a.txt");

        // Act
        Result result = _sut.Push(_clone);

        // Assert
        Assert.StartsWith("git push failed (ExitCode=1): ", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateTrackingRef_WhenTheCommitExists_MovesTheTrackingBranchThere()
    {
        // Arrange
        string newCommit = CommitFile(_clone, "a.txt");

        // Act
        Result result = _sut.UpdateTrackingRef(_clone, "main", newCommit);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newCommit, GitTestRepositories.RunGit("-C", _clone, "rev-parse", "refs/remotes/origin/main"));
    }

    [Fact]
    public void UpdateTrackingRef_WhenTheCommitIsUnknown_Fails()
    {
        // Act
        Result result = _sut.UpdateTrackingRef(_clone, "main", new string('1', 40));

        // Assert
        Assert.StartsWith("git update-ref refs/remotes/origin/main 1111", result.Error.Description,
            StringComparison.Ordinal);
    }

    //git creates the missing parent folders of the clone
    [Fact]
    public void Clone_WhenTheAddressIsARepository_ClonesItIntoTheFolder()
    {
        // Arrange
        string folder = Path.Combine(_tempFolder, "Projects", "New", "RepoA");

        // Act
        Result result = _sut.Clone(_remote, folder);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(GitTestRepositories.RunGit("-C", _remote, "rev-parse", "main"), Head(folder));
    }

    //after "--" an address that starts with "-" is a repository that does not exist, not an option of git clone
    [Fact]
    public void Clone_WhenTheAddressLooksLikeAnOption_DoesNotRunIt()
    {
        // Arrange
        string marker = Path.Combine(_tempFolder, "marker");
        string folder = Path.Combine(_tempFolder, "Projects", "Option");

        // Act
        Result result = _sut.Clone($"--upload-pack=echo x>\"{marker}\"", folder);

        // Assert
        Assert.StartsWith("git clone -- --upload-pack=", result.Error.Description, StringComparison.Ordinal);
        Assert.False(File.Exists(marker));
    }

    [Fact]
    public void CreateStartInfo_WhenAFolderIsGiven_RunsGitThereAndKeepsItFromAsking()
    {
        // Act
        ProcessStartInfo startInfo = _sut.CreateStartInfo(@"C:\Work\Repo A", ["status", "--porcelain"]);

        // Assert
        Assert.Equal("git", startInfo.FileName);
        Assert.Equal([@"-C", @"C:\Work\Repo A", "status", "--porcelain"], startInfo.ArgumentList);
        Assert.Empty(startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardInput);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Equal(Encoding.UTF8, startInfo.StandardOutputEncoding);
        Assert.Equal(Encoding.UTF8, startInfo.StandardErrorEncoding);
        Assert.True(startInfo.CreateNoWindow);
        Assert.Equal("0", startInfo.Environment["GIT_TERMINAL_PROMPT"]);
        Assert.Equal("no", startInfo.Environment["GIT_MERGE_AUTOEDIT"]);
    }

    [Fact]
    public void CreateStartInfo_WhenNoFolderIsGiven_PassesOnlyTheArguments()
    {
        // Act
        ProcessStartInfo startInfo = _sut.CreateStartInfo(null, ["clone", "--", "a", "b"]);

        // Assert
        Assert.Equal(["clone", "--", "a", "b"], startInfo.ArgumentList);
    }

    [Theory]
    [InlineData(@"C:\Program Files\Git\cmd\git.exe", @"C:\Program Files\Git\cmd\git.exe")]
    [InlineData("   ", "git")]
    public void CreateStartInfo_WhenAGitPathIsGiven_RunsThatGit(string gitExecutablePath, string expected)
    {
        // Arrange
        var sut = new GitFolderCommands(gitExecutablePath);

        // Act
        ProcessStartInfo startInfo = sut.CreateStartInfo(null, ["--version"]);

        // Assert
        Assert.Equal(expected, startInfo.FileName);
    }

    private string Clone(string name)
    {
        string folder = Path.Combine(_tempFolder, name);
        GitTestRepositories.RunGit("clone", "-q", _remote, folder);
        GitTestRepositories.ConfigureRepository(folder);
        return folder;
    }

    private void CommitIgnoredFile(string relativePath)
    {
        string filePath = Path.Combine(_clone, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, relativePath);
        GitTestRepositories.RunGit("-C", _clone, "add", "-f", "--", relativePath);
        GitTestRepositories.RunGit("-C", _clone, "commit", "-q", "-m", relativePath);
    }

    private static string CommitFile(string folder, string fileName)
    {
        File.WriteAllText(Path.Combine(folder, fileName), fileName);
        GitTestRepositories.RunGit("-C", folder, "add", "--all");
        GitTestRepositories.RunGit("-C", folder, "commit", "-q", "-m", fileName);
        return Head(folder);
    }

    private string PushNewCommitFromAnotherClone(string fileName)
    {
        string other = Clone("Other");
        string commit = CommitFile(other, fileName);
        GitTestRepositories.RunGit("-C", other, "push", "-q", "origin", "HEAD:main");
        return commit;
    }

    private static string Head(string folder)
    {
        return GitTestRepositories.RunGit("-C", folder, "rev-parse", "HEAD");
    }
}
