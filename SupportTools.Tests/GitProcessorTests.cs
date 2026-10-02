using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LibGitWork;
using Microsoft.Extensions.Logging;
using Moq;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests;

//runs the real git over throwaway repositories: a bare "remote" with one commit and its clones. GetGitState writes
//some of its messages straight to the console
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitProcessorTests : IDisposable
{
    private const string SeedContent = "seed";
    private const string SeedFileName = "Readme.txt";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public GitProcessorTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        DeleteFolder(_tempFolder);
    }

    //the address and the folder are single arguments after the end of the options: an address starting with "-"
    //is not read as an option
    [Theory]
    [InlineData("git@github.com:x/a.git")]
    [InlineData("--upload-pack=touch pwned")]
    [InlineData("-oProxyCommand=calc")]
    public void CreateCloneStartInfo_WhenCalled_PassesTheAddressAndTheFolderAfterTheEndOfOptions(string remoteAddress)
    {
        // Arrange
        string projectPath = Path.Combine(_tempFolder, "with space", "Project");

        // Act
        ProcessStartInfo startInfo = new GitProcessor(false, null, projectPath).CreateCloneStartInfo(remoteAddress);

        // Assert
        Assert.Equal("git", startInfo.FileName);
        Assert.Equal(["clone", "--", remoteAddress, projectPath], startInfo.ArgumentList);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.False(startInfo.UseShellExecute);
    }

    [Theory]
    [InlineData(null, "git")]
    [InlineData("", "git")]
    [InlineData("  ", "git")]
    [InlineData("C:\\Tools\\Git\\git.exe", "C:\\Tools\\Git\\git.exe")]
    public void CreateCloneStartInfo_WhenGitPathIsGiven_RunsThatGit(string? gitExecutablePath, string expected)
    {
        // Act
        ProcessStartInfo startInfo =
            new GitProcessor(false, null, _tempFolder, gitExecutablePath)
                .CreateCloneStartInfo("git@github.com:x/a.git");

        // Assert
        Assert.Equal(expected, startInfo.FileName);
    }

    [Fact]
    public void Clone_WhenAddressIsARepository_ClonesItAndStoresTheRemoteId()
    {
        // Arrange
        string remote = CreateRemote();
        string projectPath = Path.Combine(_tempFolder, "Projects", "My Project");
        GitProcessor sut = CreateSut(projectPath);

        // Act
        bool result = sut.Clone(remote);

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(Path.Combine(projectPath, SeedFileName)));
        Assert.Equal(WithNewLine(RunGit("-C", remote, "rev-parse", "main")), sut.LastRemoteId);
    }

    [Fact]
    public void Clone_WhenAddressIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string projectPath = Path.Combine(_tempFolder, "Project");
        string missingRepository = Path.Combine(_tempFolder, "missing.git");

        // Act
        bool result = CreateSut(projectPath).Clone(missingRepository);

        // Assert
        Assert.False(result);
        Assert.False(Directory.Exists(projectPath));
        VerifyLogged(LogLevel.Error, $"cannot clone {missingRepository} to {projectPath}");
    }

    [Fact]
    public void GetGitState_WhenCloneIsUpToDate_ReturnsUpToDate()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        GitProcessor sut = CreateSut(clone);

        // Act
        GitState result = sut.GetGitState();

        // Assert
        Assert.Equal(GitState.UpToDate, result);
        Assert.Equal(WithNewLine(RunGit("-C", clone, "rev-parse", "@")), sut.LastRemoteId);
        VerifyLogged(LogLevel.Information, $"{clone} Up to date");
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void GetGitState_WhenRemoteHasNewCommits_ReturnsNeedToPull()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        string otherHead = PushNewCommit(remote, "Other.txt");
        RunGit("-C", clone, "fetch");
        GitProcessor sut = CreateSut(clone);

        // Act
        GitState result = sut.GetGitState();

        // Assert
        Assert.Equal(GitState.NeedToPull, result);
        Assert.Equal(WithNewLine(otherHead), sut.LastRemoteId);
        Assert.Equal(WithNewLine("need to pull"), _consoleOutput.ToString());
    }

    [Fact]
    public void GetGitState_WhenLocalHasNewCommits_ReturnsNeedToPush()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        CommitFile(clone, "Mine.txt");
        GitProcessor sut = CreateSut(clone);

        // Act
        GitState result = sut.GetGitState();

        // Assert
        Assert.Equal(GitState.NeedToPush, result);
        Assert.Equal(WithNewLine(RunGit("-C", remote, "rev-parse", "main")), sut.LastRemoteId);
        Assert.Equal(WithNewLine("need to push"), _consoleOutput.ToString());
    }

    [Fact]
    public void GetGitState_WhenBothSidesHaveNewCommits_ReturnsDiverged()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        PushNewCommit(remote, "Other.txt");
        CommitFile(clone, "Mine.txt");
        RunGit("-C", clone, "fetch");

        // Act
        GitState result = CreateSut(clone).GetGitState();

        // Assert
        Assert.Equal(GitState.Diverged, result);
        VerifyLogged(LogLevel.Warning, "Diverged");
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void GetGitState_WhenFolderIsNotARepository_ReturnsUnknown()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");
        GitProcessor sut = CreateSut(folder);

        // Act
        GitState result = sut.GetGitState();

        // Assert
        Assert.Equal(GitState.Unknown, result);
        Assert.Null(sut.LastRemoteId);
        VerifyLogged(LogLevel.Error, "git rev-parse @ Error");
        //the state is known to be unknown after the first failure: git is not asked any further
        VerifyNotLogged("git rev-parse @{u} Error");
    }

    [Fact]
    public void GetGitState_WhenBranchHasNoUpstream_ReturnsUnknown()
    {
        // Arrange
        string repository = InitRepository("Local");
        CommitFile(repository, SeedFileName);
        GitProcessor sut = CreateSut(repository);

        // Act
        GitState result = sut.GetGitState();

        // Assert
        Assert.Equal(GitState.Unknown, result);
        Assert.Null(sut.LastRemoteId);
        VerifyLogged(LogLevel.Error, "git rev-parse @{u} Error");
        VerifyNotLogged("git merge-base @ @{u} Error");
    }

    [Fact]
    public void GetGitState_WhenBranchSharesNoHistoryWithItsUpstream_ReturnsUnknown()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        RunGit("-C", clone, "checkout", "--orphan", "lonely");
        CommitFile(clone, "Lonely.txt");
        RunGit("-C", clone, "branch", "--set-upstream-to=origin/main");
        GitProcessor sut = CreateSut(clone);

        // Act
        GitState result = sut.GetGitState();

        // Assert
        Assert.Equal(GitState.Unknown, result);
        Assert.Equal(WithNewLine(RunGit("-C", remote, "rev-parse", "main")), sut.LastRemoteId);
        VerifyLogged(LogLevel.Error, "git merge-base @ @{u} Error");
    }

    [Fact]
    public void CheckRemoteId_WhenBranchHasAnUpstream_StoresItsId()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        string otherHead = PushNewCommit(remote, "Other.txt");
        RunGit("-C", clone, "fetch");
        GitProcessor sut = CreateSut(clone);

        // Act
        sut.CheckRemoteId();

        // Assert
        Assert.Equal(WithNewLine(otherHead), sut.LastRemoteId);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void CheckRemoteId_WhenBranchHasNoUpstream_StoresNoId()
    {
        // Arrange
        string repository = InitRepository("Local");
        CommitFile(repository, SeedFileName);
        GitProcessor sut = CreateSut(repository);

        // Act
        sut.CheckRemoteId();

        // Assert
        Assert.Null(sut.LastRemoteId);
        VerifyLogged(LogLevel.Error, "git rev-parse @{u} Error");
    }

    [Fact]
    public void GitRemoteUpdate_WhenRemoteHasNewCommits_FetchesThem()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        string otherHead = PushNewCommit(remote, "Other.txt");

        // Act
        bool result = CreateSut(clone).GitRemoteUpdate();

        // Assert
        Assert.True(result);
        Assert.Equal(otherHead, RunGit("-C", clone, "rev-parse", "origin/main"));
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void GitRemoteUpdate_WhenFolderIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        bool result = CreateSut(folder).GitRemoteUpdate();

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, $"cannot run remote update for folder {folder}");
    }

    [Fact]
    public void Pull_WhenRemoteHasNewCommits_BringsThemIn()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        string otherHead = PushNewCommit(remote, "Other.txt");

        // Act
        bool result = CreateSut(clone).Pull();

        // Assert
        Assert.True(result);
        Assert.Equal(otherHead, RunGit("-C", clone, "rev-parse", "@"));
    }

    [Fact]
    public void Pull_WhenFolderIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        bool result = CreateSut(folder).Pull();

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, "cannot pull");
    }

    [Fact]
    public void GetRemoteOriginUrl_WhenCloned_ReturnsTheOriginAddressWithoutTheLineBreak()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");

        // Act
        Result<string> result = CreateSut(clone).GetRemoteOriginUrl();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(RunGit("-C", clone, "config", "--get", "remote.origin.url"), result.Value);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void GetRemoteOriginUrl_WhenThereIsNoOrigin_ReturnsTheError()
    {
        // Arrange
        string repository = InitRepository("Local");

        // Act
        Result<string> result = CreateSut(repository).GetRemoteOriginUrl();

        // Assert
        Assert.True(result.IsFailure);
    }

    //the message reaches git as one argument, whatever quotes and backslashes it holds
    [Fact]
    public void Commit_WhenChangesAreStaged_CommitsThemWithTheExactMessage()
    {
        // Arrange
        const string commitMessage = "Fix \"quoted\" name in C:\\Temp\\ folder\\";
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, "New.txt"), "new");
        RunGit("-C", clone, "add", "--", "New.txt");

        // Act
        bool result = CreateSut(clone).Commit(commitMessage);

        // Assert
        Assert.True(result);
        Assert.Equal(commitMessage, RunGit("-C", clone, "log", "-1", "--format=%B"));
        Assert.Equal(string.Empty, RunGit("-C", clone, "status", "--porcelain"));
    }

    [Fact]
    public void Commit_WhenNothingIsStaged_ReportsItAndReturnsFalse()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");

        // Act
        bool result = CreateSut(clone).Commit("Nothing to commit");

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, $"cannot run commit for folder {clone}");
    }

    [Fact]
    public void NeedCommit_WhenWorkingTreeIsClean_ReturnsFalse()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");

        // Act
        Result<bool> result = CreateSut(clone).NeedCommit();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void NeedCommit_WhenTrackedFileChanged_ReturnsTrue()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, SeedFileName), "changed");

        // Act
        Result<bool> result = CreateSut(clone).NeedCommit();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void NeedCommit_WhenFolderIsNotARepository_ReturnsTheError()
    {
        // Act
        Result<bool> result = CreateSut(CreateFolder("NotARepository")).NeedCommit();

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Add_WhenFolderHasNewFiles_StagesThem()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, "New.txt"), "new");

        // Act
        bool result = CreateSut(clone).Add();

        // Assert
        Assert.True(result);
        Assert.Equal("A  New.txt", RunGit("-C", clone, "status", "--porcelain"));
    }

    [Fact]
    public void Add_WhenFolderIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        bool result = CreateSut(folder).Add();

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, $"cannot run add for folder {folder}");
    }

    [Fact]
    public void Reset_WhenFilesAreStaged_UnstagesThem()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, "New.txt"), "new");
        RunGit("-C", clone, "add", "--", "New.txt");

        // Act
        bool result = CreateSut(clone).Reset();

        // Assert
        Assert.True(result);
        Assert.Equal("?? New.txt", RunGit("-C", clone, "status", "--porcelain"));
    }

    [Fact]
    public void Reset_WhenFolderIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        bool result = CreateSut(folder).Reset();

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, $"cannot run reset for folder {folder}");
    }

    [Fact]
    public void Checkout_WhenTrackedFileChanged_RestoresIt()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, SeedFileName), "changed");

        // Act
        bool result = CreateSut(clone).Checkout();

        // Assert
        Assert.True(result);
        Assert.Equal(SeedContent, File.ReadAllText(Path.Combine(clone, SeedFileName)));
    }

    [Fact]
    public void Checkout_WhenFolderIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        bool result = CreateSut(folder).Checkout();

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, $"cannot run checkout for folder {folder}");
    }

    [Fact]
    public void Clean_fdx_WhenFolderHasUntrackedAndIgnoredFiles_RemovesThem()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, ".gitignore"), "*.log");
        string untrackedFolder = CreateFolder(Path.Combine(clone, "Untracked"));
        File.WriteAllText(Path.Combine(untrackedFolder, "Notes.txt"), "notes");
        File.WriteAllText(Path.Combine(clone, "Build.log"), "log");

        // Act
        bool result = CreateSut(clone).Clean_fdx();

        // Assert
        Assert.True(result);
        Assert.Equal([".git", SeedFileName],
            Directory.EnumerateFileSystemEntries(clone).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Clean_fdx_WhenFolderIsNotARepository_ReportsItAndReturnsFalse()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        bool result = CreateSut(folder).Clean_fdx();

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, $"cannot run clean -fdx for folder {folder}");
    }

    [Fact]
    public void HaveUnTrackedFiles_WhenWorkingTreeIsClean_ReturnsFalse()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");

        // Act
        Result<bool> result = CreateSut(clone).HaveUnTrackedFiles();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void HaveUnTrackedFiles_WhenFolderHasANewFile_ReturnsTrue()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, "New.txt"), "new");

        // Act
        Result<bool> result = CreateSut(clone).HaveUnTrackedFiles();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void HaveUnTrackedFiles_WhenFolderIsNotARepository_ReturnsTheError()
    {
        // Act
        Result<bool> result = CreateSut(CreateFolder("NotARepository")).HaveUnTrackedFiles();

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void IsGitInitialized_WhenFolderIsARepository_ReturnsTrue()
    {
        // Arrange
        string repository = InitRepository("Local");

        // Act
        bool result = CreateSut(repository).IsGitInitialized();

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    [Fact]
    public void IsGitInitialized_WhenFolderIsNotARepository_ReturnsFalse()
    {
        // Act
        bool result = CreateSut(CreateFolder("NotARepository")).IsGitInitialized();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Initialise_WhenFolderExists_CreatesARepositoryInIt()
    {
        // Arrange
        string folder = CreateFolder("New Repository");

        // Act
        Result result = CreateSut(folder).Initialise();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(Directory.Exists(Path.Combine(folder, ".git")));
    }

    [Fact]
    public void Initialise_WhenFolderDoesNotExist_ReturnsTheError()
    {
        // Act
        Result result = CreateSut(Path.Combine(_tempFolder, "Missing", "Repository")).Initialise();

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void IsFolderPartOfGitWorkingTree_WhenFolderIsInsideAWorkingTree_ReturnsTrue()
    {
        // Arrange
        string repository = InitRepository("Local");
        string subFolder = CreateFolder(Path.Combine(repository, "Sub Folder"));
        GitProcessor sut = CreateSut(_tempFolder);

        // Act
        bool rootResult = sut.IsFolderPartOfGitWorkingTree(repository);
        bool subFolderResult = sut.IsFolderPartOfGitWorkingTree(subFolder);

        // Assert
        Assert.True(rootResult);
        Assert.True(subFolderResult);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    //git answers "not a git repository" with the exit code 128, which is an expected answer here, not an error
    [Fact]
    public void IsFolderPartOfGitWorkingTree_WhenFolderIsNotARepository_ReturnsFalseWithoutAnError()
    {
        // Act
        bool result = CreateSut(_tempFolder).IsFolderPartOfGitWorkingTree(CreateFolder("NotARepository"));

        // Assert
        Assert.False(result);
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    //a bare repository has no working tree: git answers "false"
    [Fact]
    public void IsFolderPartOfGitWorkingTree_WhenFolderIsABareRepository_ReturnsFalse()
    {
        // Arrange
        string remote = CreateRemote();

        // Act
        bool result = CreateSut(_tempFolder).IsFolderPartOfGitWorkingTree(remote);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFolderPartOfGitWorkingTree_WhenGitFailsWithAnotherExitCode_ReturnsFalse()
    {
        // Arrange
        string repository = InitRepository("Local");
        var sut = new GitProcessor(false, _logger.Object, _tempFolder, CreateFailingGit(1));

        // Act
        bool result = sut.IsFolderPartOfGitWorkingTree(repository);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SyncRemote_WhenCloneIsUpToDate_ReturnsSuccessWithoutPushing()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");

        // Act
        (bool success, bool pushed) = CreateSut(clone).SyncRemote();

        // Assert
        Assert.True(success);
        Assert.False(pushed);
    }

    [Fact]
    public void SyncRemote_WhenRemoteCannotBeUpdated_ReturnsFailure()
    {
        // Arrange
        string folder = CreateFolder("NotARepository");

        // Act
        (bool success, bool pushed) = CreateSut(folder).SyncRemote();

        // Assert
        Assert.False(success);
        Assert.False(pushed);
        VerifyLogged(LogLevel.Error, $"cannot run remote update for folder {folder}");
    }

    [Fact]
    public void SyncRemote_WhenLocalHasNewCommits_PushesThem()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        CommitFile(clone, "Mine.txt");

        // Act
        (bool success, bool pushed) = CreateSut(clone).SyncRemote();

        // Assert
        Assert.True(success);
        Assert.True(pushed);
        Assert.Equal(RunGit("-C", clone, "rev-parse", "@"), RunGit("-C", remote, "rev-parse", "main"));
    }

    //the remote is updated first, so the commits pushed from elsewhere are found without a fetch
    [Fact]
    public void SyncRemote_WhenRemoteHasNewCommits_PullsThem()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        string otherHead = PushNewCommit(remote, "Other.txt");

        // Act
        (bool success, bool pushed) = CreateSut(clone).SyncRemote();

        // Assert
        Assert.True(success);
        Assert.False(pushed);
        Assert.Equal(otherHead, RunGit("-C", clone, "rev-parse", "@"));
    }

    //the pull joins both sides (here with a rebase), then the next round pushes the result
    [Fact]
    public void SyncRemote_WhenBothSidesHaveNewCommits_PullsAndPushes()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        PushNewCommit(remote, "Other.txt");
        CommitFile(clone, "Mine.txt");

        // Act
        (bool success, bool pushed) = CreateSut(clone).SyncRemote();

        // Assert
        Assert.True(success);
        Assert.True(pushed);
        Assert.Equal(RunGit("-C", clone, "rev-parse", "@"), RunGit("-C", remote, "rev-parse", "main"));
        Assert.Equal(["Change Mine.txt", "Change Other.txt", $"Change {SeedFileName}"],
            RunGit("-C", remote, "log", "--format=%s", "main").Split('\n').Select(x => x.Trim()));
    }

    [Fact]
    public void SyncRemote_WhenStateIsUnknown_ReportsItAndReturnsFailure()
    {
        // Arrange
        string repository = InitRepository("Local");
        CommitFile(repository, SeedFileName);

        // Act
        (bool success, bool pushed) = CreateSut(repository).SyncRemote();

        // Assert
        Assert.False(success);
        Assert.False(pushed);
        VerifyLogged(LogLevel.Error, $"{repository} Unknown state");
    }

    //with a known remote id the remote is not updated: the state comes from the remote-tracking branch
    [Fact]
    public void SyncRemote_WhenPushFails_ReturnsFailure()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        CommitFile(clone, "Mine.txt");
        GitProcessor sut = CreateSut(clone);
        sut.CheckRemoteId();
        DeleteFolder(remote);

        // Act
        (bool success, bool pushed) = sut.SyncRemote();

        // Assert
        Assert.False(success);
        Assert.False(pushed);
        VerifyLogged(LogLevel.Error, "cannot push");
    }

    [Fact]
    public void SyncRemote_WhenPullFails_ReturnsFailure()
    {
        // Arrange
        string remote = CreateRemote();
        string clone = CloneRemote(remote, "Mine");
        PushNewCommit(remote, "Other.txt");
        RunGit("-C", clone, "fetch");
        GitProcessor sut = CreateSut(clone);
        sut.CheckRemoteId();
        DeleteFolder(remote);

        // Act
        (bool success, bool pushed) = sut.SyncRemote();

        // Assert
        Assert.False(success);
        Assert.False(pushed);
        VerifyLogged(LogLevel.Error, "cannot pull");
    }

    [Fact]
    public void GetRedundantCachedFilesList_WhenNoTrackedFileIsIgnored_ReturnsNoNames()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, ".gitignore"), "*.log");

        // Act
        Result<string[]> result = CreateSut(clone).GetRedundantCachedFilesList();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(string.Empty, _consoleOutput.ToString());
    }

    //git prints a non-ASCII name quoted, with octal bytes. The output ends with a line break, so the list ends with
    //an empty name, which the caller skips
    [Fact]
    public void GetRedundantCachedFilesList_WhenTrackedFilesAreIgnored_ReturnsTheirRealNames()
    {
        // Arrange
        string clone = CloneRemote(CreateRemote(), "Mine");
        File.WriteAllText(Path.Combine(clone, ".gitignore"), "*.log");
        File.WriteAllText(Path.Combine(clone, "app.log"), "log");
        File.WriteAllText(Path.Combine(clone, "\u10e4.log"), "log");
        RunGit("-C", clone, "add", "-f", "--", "app.log", "\u10e4.log");

        // Act
        Result<string[]> result = CreateSut(clone).GetRedundantCachedFilesList();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["app.log", "\u10e4.log", string.Empty], result.Value);
    }

    [Fact]
    public void GetRedundantCachedFilesList_WhenFolderIsNotARepository_ReturnsTheError()
    {
        // Act
        Result<string[]> result = CreateSut(CreateFolder("NotARepository")).GetRedundantCachedFilesList();

        // Assert
        Assert.True(result.IsFailure);
    }

    //without "--" git read such a file name as options ("unknown switch")
    [Fact]
    public void RemoveFromCacheRedundantCachedFile_WhenFileNameStartsWithADash_RemovesItFromTheIndex()
    {
        // Arrange
        const string fileName = "-dash.txt";
        string repository = InitRepository("Local");
        File.WriteAllText(Path.Combine(repository, fileName), "content");
        RunGit("-C", repository, "add", "--", fileName);

        // Act
        bool result = CreateSut(repository).RemoveFromCacheRedundantCachedFile(fileName);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, RunGit("-C", repository, "ls-files"));
        Assert.True(File.Exists(Path.Combine(repository, fileName)));
    }

    [Fact]
    public void RemoveFromCacheRedundantCachedFile_WhenFileIsNotTracked_ReportsItAndReturnsFalse()
    {
        // Arrange
        string repository = InitRepository("Local");

        // Act
        bool result = CreateSut(repository).RemoveFromCacheRedundantCachedFile("Missing.txt");

        // Assert
        Assert.False(result);
        VerifyLogged(LogLevel.Error, "cannot remove file Missing.txt from cache");
    }

    [Theory]
    [InlineData("\"\\341\\203\\244.log\"", "\u10e4.log")]
    [InlineData("\"a\\033b\"", "a\u001bb")]
    [InlineData("\"\\a\\b\\t\\n\\v\\f\\r\"", "\a\b\t\n\v\f\r")]
    [InlineData("\"say \\\"hi\\\" \\\\ there\"", "say \"hi\" \\ there")]
    [InlineData("\"\\q\"", "q")]
    [InlineData("\"plain name\"", "plain name")]
    [InlineData("\"\"", "")]
    public void UnquoteGitPath_WhenNameIsQuoted_RestoresIt(string quotedName, string expected)
    {
        // Act
        string result = GitProcessor.UnquoteGitPath(quotedName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("plain.txt")]
    [InlineData("")]
    [InlineData("\"")]
    [InlineData("\"open.txt")]
    [InlineData("close.txt\"")]
    public void UnquoteGitPath_WhenNameIsNotQuoted_KeepsIt(string name)
    {
        // Act
        string result = GitProcessor.UnquoteGitPath(name);

        // Assert
        Assert.Equal(name, result);
    }

    [Theory]
    [InlineData("plain", "\"plain\"")]
    [InlineData("", "\"\"")]
    [InlineData("with space", "\"with space\"")]
    [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
    [InlineData("C:\\Temp\\file.txt", "\"C:\\Temp\\file.txt\"")]
    [InlineData("C:\\Temp\\", "\"C:\\Temp\\\\\"")]
    [InlineData("end\\\\", "\"end\\\\\\\\\"")]
    [InlineData("a\\\"b", "\"a\\\\\\\"b\"")]
    [InlineData("x\\y\"z", "\"x\\y\\\"z\"")]
    public void QuoteArgument_WhenCalled_QuotesItByTheCommandLineRules(string argument, string expected)
    {
        // Act
        string result = GitProcessor.QuoteArgument(argument);

        // Assert
        Assert.Equal(expected, result);
    }

    private GitProcessor CreateSut(string projectPath)
    {
        return new GitProcessor(false, _logger.Object, projectPath);
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

    private void VerifyNotLogged(string message)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == message), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
#pragma warning restore CA1873
    }

    //a bare repository with one commit on main, the way a hosting service keeps it
    private string CreateRemote()
    {
        string seed = InitRepository("Seed");
        CommitFile(seed, SeedFileName, SeedContent);
        string remote = Path.Combine(_tempFolder, "Remote", "remote.git");
        RunGit("clone", "--bare", seed, remote);
        return remote;
    }

    //commits a new file in another clone of the remote and pushes it; returns the new remote id
    private string PushNewCommit(string remote, string fileName)
    {
        string other = CloneRemote(remote, "Other");
        CommitFile(other, fileName);
        RunGit("-C", other, "push");
        return RunGit("-C", other, "rev-parse", "@");
    }

    private string CloneRemote(string remote, string name)
    {
        string clone = Path.Combine(_tempFolder, "with space", name);
        RunGit("clone", remote, clone);
        ConfigureRepository(clone);
        return clone;
    }

    private string InitRepository(string name)
    {
        string repository = Path.Combine(_tempFolder, "with space", name);
        RunGit("init", "--initial-branch=main", repository);
        ConfigureRepository(repository);
        return repository;
    }

    private string CreateFolder(string name)
    {
        return Directory.CreateDirectory(Path.Combine(_tempFolder, name)).FullName;
    }

    //a stand-in for git that fails every command with the given exit code
    private string CreateFailingGit(int exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            string commandFilePath = Path.Combine(_tempFolder, "failing-git.cmd");
            File.WriteAllText(commandFilePath, $"@exit /b {exitCode}");
            return commandFilePath;
        }

        string scriptFilePath = Path.Combine(_tempFolder, "failing-git.sh");
        File.WriteAllText(scriptFilePath, $"#!/bin/sh\nexit {exitCode}\n");
        File.SetUnixFileMode(scriptFilePath, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        return scriptFilePath;
    }

    //the tests must not depend on the global git configuration of the machine
    private static void ConfigureRepository(string repository)
    {
        RunGit("-C", repository, "config", "user.name", "Test User");
        RunGit("-C", repository, "config", "user.email", "test.user@example.com");
        RunGit("-C", repository, "config", "commit.gpgsign", "false");
        RunGit("-C", repository, "config", "pull.rebase", "true");
    }

    private static void CommitFile(string repository, string fileName, string content = "content")
    {
        File.WriteAllText(Path.Combine(repository, fileName), content);
        RunGit("-C", repository, "add", "--", fileName);
        RunGit("-C", repository, "commit", "-m", $"Change {fileName}");
    }

    private static string WithNewLine(string text)
    {
        return $"{text}{Environment.NewLine}";
    }

    private static string RunGit(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var errorOutput = new StringBuilder();
        using var git = new Process();
        git.StartInfo = startInfo;
        git.ErrorDataReceived += (_, e) => errorOutput.AppendLine(e.Data);
        git.Start();
        git.BeginErrorReadLine();
        string output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        Assert.True(git.ExitCode == 0, $"git {string.Join(' ', arguments)} failed: {errorOutput}");
        return output.Trim();
    }

    //git keeps its object files read-only, Directory.Delete does not remove such files
    private static void DeleteFolder(string folder)
    {
        foreach (string filePath in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        Directory.Delete(folder, true);
    }
}
