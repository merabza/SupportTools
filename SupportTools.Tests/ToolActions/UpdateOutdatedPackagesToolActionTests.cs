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

//no git and no dotnet outdated run: WorkFolder is not set (the syncs stop before git), the gits that exist are Node
//gits (their package update returns at once), and a git that is not in Gits fails like a failed dotnet outdated.
//G1: P1 [GitA], P2 [GitA, MissingB], P3 [MissingP3], P5 [MissingX, MissingY]; G2: R1 (remote) [MissingR1]
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateOutdatedPackagesToolActionTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<(string FieldName, string DefaultValue)> _inputs = [];
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public UpdateOutdatedPackagesToolActionTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Gits["GitA"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/GitA.git", GitProjectFolderName = "GitA", GitIgnorePatternName = "Node"
        };
        AddProject("P1", "G1", true, "GitA");
        AddProject("P2", "G1", true, "GitA", "MissingB");
        AddProject("P3", "G1", true, "MissingP3");
        AddProject("P5", "G1", true, "MissingX", "MissingY");
        AddProject("R1", "G2", false, "MissingR1");
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public async Task Run_WhenCalled_AsksForTheMessageWithADefault()
    {
        // Act
        await Create(null, null, false).Run();

        // Assert
        (string fieldName, string defaultValue) = Assert.Single(_inputs);
        Assert.Equal("Message", fieldName);
        Assert.Matches("^Update Packages [0-9]{12}$", defaultValue);
    }

    //the single-git projects go first; a git done for one project is removed from the others, so P2 waits for GitA
    //and goes in the second round; P5 never gets down to one git and is not processed
    [Fact]
    public async Task Run_WhenSomeGitsFail_ProcessesSingleGitProjectsRoundByRoundAndReportsTheFailures()
    {
        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("dotnet outdated failed for: MissingP3, MissingB" + Environment.NewLine, output,
            StringComparison.Ordinal);
        //one check sync of every project first, then a sync before and after the update of each processed project
        Assert.Equal(7, CountOccurrences(output, "Sync Multiple Projects Gits V2 Started"));
    }

    //the gits are read with the console by every sync and by the package update of a project
    [Fact]
    public async Task Run_WhenAGitHasNoAddress_ReportsItOnTheConsoleForEverySyncAndTheUpdate()
    {
        // Arrange
        _parameters.Projects.Remove("P2");
        _parameters.Projects.Remove("P3");
        _parameters.Projects.Remove("P5");
        _parameters.Gits["NoAddress"] = new GitDataModel { GitProjectFolderName = "NoAddress", GitIgnorePatternName = "Node" };

        // Act
        await Create(null, null, false).Run();

        // Assert
        Assert.Equal(4, CountOccurrences(_consoleOutput.ToString(),
            "GitProjectAddress is empty for Git Repo with key NoAddress)"));
    }

    //the check sync commits the local changes with the given message and the "Check" suffix (real git, local remote)
    [Fact]
    public async Task Run_WhenAGitHasLocalChanges_TheCheckSyncCommitsThemWithTheCheckMessage()
    {
        // Arrange
        string remote = GitTestRepositories.CreateRemote(_tempFolder, "GitR",
            new Dictionary<string, string> { [".gitignore"] = "bin/", ["Readme.txt"] = "GitR" });
        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        _parameters.Gits["GitR"] = new GitDataModel
        {
            GitProjectAddress = remote, GitProjectFolderName = "GitR", GitIgnorePatternName = "Node"
        };
        _parameters.Projects.Clear();
        AddProject("PR", "G1", true, "GitR");
        string clone = Path.Combine(_tempFolder, "PR", "GitR");
        GitTestRepositories.RunGit("clone", remote, clone);
        GitTestRepositories.ConfigureRepository(clone);
        await File.WriteAllTextAsync(Path.Combine(clone, "New.txt"), "new");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        Assert.Equal("Update message Check", GitTestRepositories.RunGit("-C", remote, "log", "-1", "--format=%s"));
    }

    [Fact]
    public async Task Run_WhenRemoteProjectsAreIncluded_ProcessesThemToo()
    {
        // Act
        bool result = await Create(null, null, true).Run();

        // Assert
        Assert.False(result);
        Assert.Contains("dotnet outdated failed for: MissingP3, MissingR1, MissingB" + Environment.NewLine,
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenRemoteProjectsAreNotIncluded_TheFirstSyncSkipsThemToo()
    {
        // Act
        await Create(null, null, false).Run();

        // Assert
        Assert.DoesNotContain("Git Project with name MissingR1 does not exists in project R1",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenEveryPackageUpdateSucceeds_ReturnsTrue()
    {
        // Arrange
        _parameters.Projects.Remove("P2");
        _parameters.Projects.Remove("P3");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("Update Outdated Packages Started", output, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet outdated failed", output, StringComparison.Ordinal);
    }

    //a git done for one project leaves another single-git project of the same round with nothing to do
    [Fact]
    public async Task Run_WhenTwoSingleGitProjectsShareTheGit_UpdatesItOnce()
    {
        // Arrange
        _parameters.Projects.Remove("P2");
        _parameters.Projects.Remove("P3");
        AddProject("P4", "G1", true, "GitA");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        Assert.DoesNotContain("Error when run Tool Action", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenGroupIsGiven_ProcessesOnlyItsProjects()
    {
        // Act
        bool result = await Create("G2", null, true).Run();

        // Assert
        Assert.False(result);
        Assert.Contains("dotnet outdated failed for: MissingR1" + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenProjectIsGiven_ProcessesOnlyIt()
    {
        // Act
        bool result = await Create(null, "P3", false).Run();

        // Assert
        Assert.False(result);
        Assert.Contains("dotnet outdated failed for: MissingP3" + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //the summary of the failures is logged with the logger of the action
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WhenGitWorkIsLoggedOrNot_GivesTheLoggerToTheActionOnlyWhenLogged(bool logGitWork)
    {
        // Arrange
        _parameters.LogGitWork = logGitWork;

        // Act
        await Create(null, "P3", false).Run();

        // Assert
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString() == "dotnet outdated failed for: MissingP3"),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            logGitWork ? Times.Once() : Times.Never());
#pragma warning restore CA1873
    }

    private UpdateOutdatedPackagesToolAction Create(string? projectGroupName, string? projectName,
        bool includeRemoteProjects)
    {
        return UpdateOutdatedPackagesToolAction.Create(_logger.Object, _parametersManager.Object, projectGroupName,
            projectName, includeRemoteProjects, true, (fieldName, defaultValue) =>
            {
                _inputs.Add((fieldName, defaultValue));
                return "Update message";
            });
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }

    private void AddProject(string projectName, string groupName, bool isLocal, params string[] gitNames)
    {
        string projectFolder = Path.Combine(_tempFolder, projectName);
        if (isLocal)
        {
            Directory.CreateDirectory(projectFolder);
        }

        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName, ProjectFolderName = projectFolder, GitProjectNames = [.. gitNames]
        };
    }
}
