using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using LibGitData.Models;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//runs the real V2 sync over throwaway repositories (local bare remotes, so no network is needed).
//G1: Local1 (folder exists, git RepoA) and Remote1 (folder missing, git RepoB); G2: Local2 (folder exists, git RepoC)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncMultipleProjectsGitsToolActionV2Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectsFolder;
    private readonly Dictionary<string, string> _remotes = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public SyncMultipleProjectsGitsToolActionV2Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectsFolder = Path.Combine(_tempFolder, "Projects");
        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        foreach (string gitName in (string[])["RepoA", "RepoB", "RepoC"])
        {
            AddGit(gitName);
        }

        AddProject("Local1", "G1", true, "RepoA");
        AddProject("Remote1", "G1", false, "RepoB");
        AddProject("Local2", "G2", true, "RepoC");
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public async Task Run_WhenRemoteProjectsAreNotIncluded_ClonesOnlyTheGitsOfLocalProjects()
    {
        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(GitFolder("Local1", "RepoA"), ".git")));
        Assert.True(Directory.Exists(Path.Combine(GitFolder("Local2", "RepoC"), ".git")));
        Assert.False(Directory.Exists(ProjectFolder("Remote1")));
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V2 Started", output, StringComparison.Ordinal);
        Assert.Contains($"Count changes{Environment.NewLine}", output, StringComparison.Ordinal);
        Assert.Contains($"Count changes finished{Environment.NewLine}", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RepoB", output, StringComparison.Ordinal);
        //the gits without changes go in name order
        Assert.True(output.IndexOf("---=== RepoA Collect", StringComparison.Ordinal) <
                    output.IndexOf("---=== RepoC Collect", StringComparison.Ordinal));
    }

    //a git without an address is reported each time the gits are read with the console: once for the sync action of
    //the project and once more by the synchroniser, which reads them for its clone in the work folder
    [Fact]
    public async Task Run_WhenAGitHasNoAddress_ReportsItOnTheConsoleForTheActionAndTheSynchroniser()
    {
        // Arrange
        _parameters.Gits["NoAddress"] =
            new GitDataModel { GitProjectFolderName = "NoAddress", GitIgnorePatternName = "CSharp" };

        // Act
        await Create(null, "Local1", false).Run();

        // Assert
        Assert.Equal(2,
            CountOccurrences(_consoleOutput.ToString(), "GitProjectAddress is empty for Git Repo with key NoAddress)"));
    }

    //a git that is not in Gits gives no sync action, so no synchroniser runs for it
    [Fact]
    public async Task Run_WhenAProjectGitIsNotInGits_SkipsIt()
    {
        // Arrange
        _parameters.Projects["Local1"].GitProjectNames.Add("MissingGit");

        // Act
        bool result = await Create(null, "Local1", false).Run();

        // Assert
        Assert.True(result);
        string output = _consoleOutput.ToString();
        Assert.DoesNotContain("git with name MissingGit does not exists", output, StringComparison.Ordinal);
        Assert.DoesNotContain("---=== MissingGit", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenRemoteProjectsAreIncluded_ClonesThemToo()
    {
        // Act
        bool result = await Create(null, null, true).Run();

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(Path.Combine(GitFolder("Remote1", "RepoB"), ".git")));
        Assert.Contains("---=== RepoB Collect", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenGroupIsGiven_SyncsOnlyItsProjects()
    {
        // Act
        await Create("G1", null, true).Run();

        // Assert
        Assert.True(Directory.Exists(GitFolder("Local1", "RepoA")));
        Assert.True(Directory.Exists(GitFolder("Remote1", "RepoB")));
        Assert.False(Directory.Exists(GitFolder("Local2", "RepoC")));
    }

    [Fact]
    public async Task Run_WhenProjectIsGiven_SyncsOnlyIt()
    {
        // Act
        await Create(null, "Local2", true).Run();

        // Assert
        Assert.True(Directory.Exists(GitFolder("Local2", "RepoC")));
        Assert.False(Directory.Exists(GitFolder("Local1", "RepoA")));
    }

    //one synchroniser per git: the remote is read once for every project that uses the git
    [Fact]
    public async Task Run_WhenTwoProjectsUseTheSameGit_SyncsTheGitOnceForBoth()
    {
        // Arrange
        _parameters.Projects["Local2"].GitProjectNames.Add("RepoA");

        // Act
        await Create(null, null, false).Run();

        // Assert
        Assert.True(Directory.Exists(GitFolder("Local1", "RepoA")));
        Assert.True(Directory.Exists(GitFolder("Local2", "RepoA")));
        string output = _consoleOutput.ToString();
        Assert.Equal(1, CountOccurrences(output, "---=== RepoA Collect"));
        //the projects of a git go in name order
        Assert.True(output.IndexOf($"Checking {GitFolder("Local1", "RepoA")}...", StringComparison.Ordinal) <
                    output.IndexOf($"Checking {GitFolder("Local2", "RepoA")}...", StringComparison.Ordinal));
    }

    //a git with local changes goes first; the given message is used for its commit without asking
    [Fact]
    public async Task Run_WhenAGitHasChanges_ReportsItCommitsWithTheGivenMessageAndProcessesItFirst()
    {
        // Arrange
        string clone = GitFolder("Local2", "RepoC");
        GitTestRepositories.RunGit("clone", _remotes["RepoC"], clone);
        GitTestRepositories.ConfigureRepository(clone);
        await File.WriteAllTextAsync(Path.Combine(clone, "New.txt"), "new");

        // Act
        bool result = await Create(null, null, false, "Sync message").Run();

        // Assert
        Assert.True(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("RepoC has changes", output, StringComparison.Ordinal);
        Assert.True(output.IndexOf("---=== RepoC Collect", StringComparison.Ordinal) <
                    output.IndexOf("---=== RepoA Collect", StringComparison.Ordinal));
        Assert.Equal("Sync message", GitTestRepositories.RunGit("-C", _remotes["RepoC"], "log", "-1", "--format=%s"));
    }

    [Fact]
    public async Task Run_WhenNoGitHasChanges_ReportsNoChanges()
    {
        // Act
        await Create(null, null, false).Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Count changes", output, StringComparison.Ordinal);
        Assert.DoesNotContain("has changes", output, StringComparison.Ordinal);
    }

    //the synchroniser logs with the logger of the action: without a work folder it cannot update its clone there
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WhenGitWorkIsLoggedOrNot_GivesTheLoggerToTheActionOnlyWhenLogged(bool logGitWork)
    {
        // Arrange
        _parameters.LogGitWork = logGitWork;
        _parameters.WorkFolder = null;

        // Act
        await Create(null, "Local1", false).Run();

        // Assert
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString() == "supportToolsParameters.WorkFolder does not specified"), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), logGitWork ? Times.Once() : Times.Never());
#pragma warning restore CA1873
    }

    private SyncMultipleProjectsGitsToolActionV2 Create(string? projectGroupName, string? projectName,
        bool includeRemoteProjects, string? useMessage = null)
    {
        return SyncMultipleProjectsGitsToolActionV2.Create(_logger.Object, _parametersManager.Object, projectGroupName,
            projectName, true, useMessage, includeRemoteProjects);
    }

    private void AddGit(string gitName)
    {
        //the sync reads the cached files that .gitignore excludes, so every repository has one
        string remote = GitTestRepositories.CreateRemote(_tempFolder, gitName,
            new Dictionary<string, string> { ["Readme.txt"] = gitName, [".gitignore"] = "bin/" });
        _remotes[gitName] = remote;
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = remote, GitProjectFolderName = gitName, GitIgnorePatternName = "CSharp"
        };
    }

    private void AddProject(string projectName, string groupName, bool isLocal, string gitName)
    {
        string projectFolder = ProjectFolder(projectName);
        if (isLocal)
        {
            Directory.CreateDirectory(projectFolder);
        }

        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName, ProjectFolderName = projectFolder, GitProjectNames = [gitName]
        };
    }

    private string ProjectFolder(string projectName)
    {
        return Path.Combine(_projectsFolder, projectName);
    }

    private string GitFolder(string projectName, string gitName)
    {
        return Path.Combine(ProjectFolder(projectName), gitName);
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }
}
