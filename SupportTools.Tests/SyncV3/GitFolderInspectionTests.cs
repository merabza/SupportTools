using System;
using System.Collections.Generic;
using System.IO;
using LibGitWork.SyncV3;
using Xunit;

namespace SupportTools.Tests.SyncV3;

//the local check of a folder, with the real git over a throwaway bare remote
public sealed class GitFolderInspectionTests : IDisposable
{
    private readonly GitFolderCommands _commands = new(null);
    private readonly string _remote;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public GitFolderInspectionTests()
    {
        _remote = GitTestRepositories.CreateRemote(_tempFolder, "RepoA",
            new Dictionary<string, string> { ["Readme.txt"] = "RepoA", [".gitignore"] = "bin/" });
    }

    public void Dispose()
    {
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public void Inspect_WhenTheFolderIsMissing_NeedsAClone()
    {
        // Act
        GitFolderInspection inspection = Inspect(Path.Combine(_tempFolder, "Missing"));

        // Assert
        Assert.Equal(EGitFolderInspectionKind.NeedsClone, inspection.Kind);
    }

    [Fact]
    public void Inspect_WhenTheFolderIsClean_IsReadyWithItsStatus()
    {
        // Arrange
        string folder = Clone("A");
        string head = GitTestRepositories.RunGit("-C", folder, "rev-parse", "HEAD");

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Ready, inspection.Kind);
        Assert.Equal(new GitFolderStatus(head, "main", false, false), inspection.Status);
    }

    //a file that .gitignore excludes, but which was committed (with add -f): the check removes it from the index,
    //which is a change to commit
    [Fact]
    public void Inspect_WhenAnIgnoredFileIsCached_RemovesItFromTheIndex()
    {
        // Arrange
        string folder = Clone("A");
        Directory.CreateDirectory(Path.Combine(folder, "bin"));
        File.WriteAllText(Path.Combine(folder, "bin", "Cached.dll"), "dll");
        GitTestRepositories.RunGit("-C", folder, "add", "-f", "--", "bin/Cached.dll");
        GitTestRepositories.RunGit("-C", folder, "commit", "-q", "-m", "Cached file");

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.True(inspection.HasChanges);
        Assert.Equal("D  bin/Cached.dll", GitTestRepositories.RunGit("-C", folder, "status", "--porcelain"));
    }

    //a new file is staged, so the commit of the sync takes it
    [Fact]
    public void Inspect_WhenThereIsANewFile_StagesIt()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "New.txt"), "new");

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.True(inspection.HasChanges);
        Assert.Equal("New.txt", GitTestRepositories.RunGit("-C", folder, "diff", "--cached", "--name-only"));
    }

    [Fact]
    public void Inspect_WhenTheFolderIsClonedFromAnotherAddress_Fails()
    {
        // Arrange
        string folder = Clone("A");
        GitTestRepositories.RunGit("-C", folder, "remote", "set-url", "origin", "git@example.com:other/RepoA.git");

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Failed, inspection.Kind);
        Assert.Equal($"is cloned from git@example.com:other/RepoA.git, not from {_remote}", inspection.ErrorMessage);
    }

    [Fact]
    public void Inspect_WhenTheRepositoryHasNoGitignore_Fails()
    {
        // Arrange
        string folder = Clone("A");
        File.Delete(Path.Combine(folder, ".gitignore"));

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Failed, inspection.Kind);
        Assert.StartsWith("git -c core.quotePath=true ls-files -i --exclude-from=.gitignore -c failed",
            inspection.ErrorMessage, StringComparison.Ordinal);
        Assert.False(inspection.HasChanges);
    }

    //another git process holds the index (index.lock): the ignored file cannot leave the index
    [Fact]
    public void Inspect_WhenTheIgnoredFileCannotLeaveTheIndex_Fails()
    {
        // Arrange
        string folder = Clone("A");
        Directory.CreateDirectory(Path.Combine(folder, "bin"));
        File.WriteAllText(Path.Combine(folder, "bin", "Cached.dll"), "dll");
        GitTestRepositories.RunGit("-C", folder, "add", "-f", "--", "bin/Cached.dll");
        GitTestRepositories.RunGit("-C", folder, "commit", "-q", "-m", "Cached file");
        File.WriteAllText(Path.Combine(folder, ".git", "index.lock"), string.Empty);

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Failed, inspection.Kind);
        Assert.StartsWith("git rm --cached -- bin/Cached.dll failed", inspection.ErrorMessage,
            StringComparison.Ordinal);
    }

    //a wrong status.showUntrackedFiles breaks git status only, so the earlier steps of the check pass
    [Fact]
    public void Inspect_WhenTheStatusCannotBeRead_Fails()
    {
        // Arrange
        string folder = Clone("A");
        GitTestRepositories.RunGit("-C", folder, "config", "status.showUntrackedFiles", "bogus");

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Failed, inspection.Kind);
        Assert.StartsWith("git status --porcelain=v2 --branch --untracked-files=normal failed",
            inspection.ErrorMessage, StringComparison.Ordinal);
    }

    //git status still works with a locked index, git add does not
    [Fact]
    public void Inspect_WhenTheChangesCannotBeStaged_Fails()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "New.txt"), "new");
        File.WriteAllText(Path.Combine(folder, ".git", "index.lock"), string.Empty);

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Failed, inspection.Kind);
        Assert.StartsWith("git add . failed", inspection.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Inspect_WhenTheFolderIsNotARepository_Fails()
    {
        // Arrange
        string folder = Directory.CreateDirectory(Path.Combine(_tempFolder, "Plain")).FullName;

        // Act
        GitFolderInspection inspection = Inspect(folder);

        // Assert
        Assert.Equal(EGitFolderInspectionKind.Failed, inspection.Kind);
        Assert.StartsWith("is not a git repository with an origin: git config --get remote.origin.url failed",
            inspection.ErrorMessage, StringComparison.Ordinal);
    }

    private GitFolderInspection Inspect(string folder)
    {
        return GitFolderInspection.Inspect(_commands, new GitSyncFolder("RepoA", _remote, "Project", folder));
    }

    private string Clone(string name)
    {
        string folder = Path.Combine(_tempFolder, name);
        GitTestRepositories.RunGit("clone", _remote, folder);
        GitTestRepositories.ConfigureRepository(folder);
        return folder;
    }
}
