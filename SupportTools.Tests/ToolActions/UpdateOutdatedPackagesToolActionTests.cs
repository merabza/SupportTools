using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LibGitData.Models;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Tests.OutdatedPackages;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//no git and no dotnet outdated run: the steps are FakeOutdatedPackagesUpdateSteps, the clock is a ManualTimeProvider.
//G1: P1 [GitA], P2 [GitA, GitB, Front (a React git)]; G2: R1 (remote) [GitC]
[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateOutdatedPackagesToolActionTests : IDisposable
{
    private const string CheckSync = "sync all: Update message Check";
    private const string ContinueQuestion = "Some gits were not synced (see above). Continue updating packages?";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<(string FieldName, string DefaultValue)> _inputs = [];
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly FakeOutdatedPackagesUpdateSteps _steps = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private readonly ManualTimeProvider _timeProvider = new();
    private bool _answer;

    public UpdateOutdatedPackagesToolActionTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        AddGit("GitA", "CSharp");
        AddGit("GitB", "CSharp");
        AddGit("GitC", "CSharp");
        AddGit("Front", "React");
        AddProject("P1", "G1", true, "GitA");
        AddProject("P2", "G1", true, "GitA", "GitB", "Front");
        AddProject("R1", "G2", false, "GitC");
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
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

    //P2 waits for GitA of P1 and does not wait for its React git
    [Fact]
    public async Task Run_WhenEveryStepSucceeds_SyncsAllGitsFirstAndEachProjectBeforeAndAfterItsUpdate()
    {
        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        Assert.Equal(
        [
            CheckSync, "sync P1: Update message Prepare", "update P1/GitA", "sync P1: Update message Update",
            "sync P2: Update message Prepare", "update P2/GitB", "sync P2: Update message Update"
        ], _steps.Steps);
        Assert.Empty(_questions);
        Assert.All(_steps.IncludeRemoteProjectsValues, Assert.False);
    }

    [Fact]
    public async Task Run_WhenTheCheckSyncFailsAndTheAnswerIsNo_StopsBeforeTheUpdates()
    {
        // Arrange
        _steps.FailingSteps.Add(CheckSync);

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        Assert.Equal([(ContinueQuestion, false)], _questions);
        Assert.Equal([CheckSync], _steps.Steps);
    }

    [Fact]
    public async Task Run_WhenTheCheckSyncFailsAndTheAnswerIsYes_UpdatesThePackages()
    {
        // Arrange
        _steps.FailingSteps.Add(CheckSync);
        _answer = true;

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.True(result);
        Assert.Equal([(ContinueQuestion, false)], _questions);
        Assert.Contains("update P2/GitB", _steps.Steps);
    }

    //a cancelled sync answers false like a failed one; the cancellation stops the run without the question
    [Fact]
    public async Task Run_WhenCancelledDuringTheCheckSync_StopsWithoutAsking()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _steps.FailingSteps.Add(CheckSync);
        _steps.OnStep = _ => cancellationTokenSource.Cancel();

        // Act
        bool result = await Create(null, null, false).Run(cancellationTokenSource.Token);

        // Assert
        Assert.False(result);
        Assert.Empty(_questions);
        Assert.Equal([CheckSync], _steps.Steps);
        Assert.Contains("Operation Canceled", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //GitA is still done for the order, so P2 updates GitB
    [Fact]
    public async Task Run_WhenASyncBeforeAnUpdateFails_SkipsThatGitAndUpdatesTheOthers()
    {
        // Arrange
        _steps.FailingSteps.Add("sync P1: Update message Prepare");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        Assert.Equal(
        [
            CheckSync, "sync P1: Update message Prepare", "sync P2: Update message Prepare", "update P2/GitB",
            "sync P2: Update message Update"
        ], _steps.Steps);
        Assert.Contains("sync before the update failed for: GitA" + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //the files that dotnet outdated already changed are synced as before
    [Fact]
    public async Task Run_WhenAPackageUpdateFails_StillSyncsTheProjectAndReportsTheGit()
    {
        // Arrange
        _steps.FailingSteps.Add("update P1/GitA");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        Assert.Contains("sync P1: Update message Update", _steps.Steps);
        Assert.Contains("dotnet outdated failed for: GitA" + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenASyncAfterAnUpdateFails_ReportsTheGitAndGoesOn()
    {
        // Arrange
        _steps.FailingSteps.Add("sync P1: Update message Update");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        Assert.Contains("update P2/GitB", _steps.Steps);
        Assert.Contains("sync after the update failed for: GitA" + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenStepsOfEveryKindFail_ReportsThemTogether()
    {
        // Arrange
        _steps.FailingSteps.Add("sync P1: Update message Prepare");
        _steps.FailingSteps.Add("update P2/GitB");
        _steps.FailingSteps.Add("sync P2: Update message Update");

        // Act
        bool result = await Create(null, null, false).Run();

        // Assert
        Assert.False(result);
        Assert.Contains(
            "sync before the update failed for: GitA" + Environment.NewLine + "dotnet outdated failed for: GitB" +
            Environment.NewLine + "sync after the update failed for: GitB" + Environment.NewLine,
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenRemoteProjectsAreIncluded_UpdatesThemToo()
    {
        // Act
        bool result = await Create(null, null, true).Run();

        // Assert
        Assert.True(result);
        Assert.Contains("update R1/GitC", _steps.Steps);
        Assert.All(_steps.IncludeRemoteProjectsValues, Assert.True);
    }

    [Fact]
    public async Task Run_WhenRemoteProjectsAreNotIncluded_LeavesThemOut()
    {
        // Act
        await Create(null, null, false).Run();

        // Assert
        Assert.DoesNotContain(_steps.Steps, x => x.Contains("R1", StringComparison.Ordinal));
    }

    //the check sync still covers every project
    [Fact]
    public async Task Run_WhenGroupIsGiven_UpdatesOnlyItsProjects()
    {
        // Act
        bool result = await Create("G2", null, true).Run();

        // Assert
        Assert.True(result);
        Assert.Equal(
            [CheckSync, "sync R1: Update message Prepare", "update R1/GitC", "sync R1: Update message Update"],
            _steps.Steps);
    }

    //alone, P2 has two C# gits left: they are updated one after the other in its folder
    [Fact]
    public async Task Run_WhenProjectIsGiven_UpdatesEveryCSharpGitOfIt()
    {
        // Act
        bool result = await Create(null, "P2", false).Run();

        // Assert
        Assert.True(result);
        Assert.Equal(
        [
            CheckSync, "sync P2: Update message Prepare", "update P2/GitA", "sync P2: Update message Update",
            "sync P2: Update message Prepare", "update P2/GitB", "sync P2: Update message Update"
        ], _steps.Steps);
    }

    [Fact]
    public async Task Run_WhenCalled_WritesTheTimeOfEveryPhase()
    {
        // Arrange
        _steps.OnStep = step => _timeProvider.Advance(step switch
        {
            CheckSync => TimeSpan.FromMinutes(3),
            _ when step.EndsWith(" Prepare", StringComparison.Ordinal) => TimeSpan.FromMinutes(1),
            _ when step.StartsWith("update ", StringComparison.Ordinal) => TimeSpan.FromSeconds(2430),
            _ => TimeSpan.FromSeconds(75)
        });

        // Act
        await Create(null, null, false).Run();

        // Assert
        Assert.Contains(string.Join(Environment.NewLine, "Times of Update Outdated Packages:",
            "  sync of all gits (Check): 00:03:00", "  2 syncs before an update (Prepare): 00:02:00",
            "  2 package updates (restore, dotnet outdated): 01:21:00",
            "  2 syncs after an update (Update): 00:02:30") + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    //a git whose folder was not synced is not updated, so only its sync before the update counts
    [Fact]
    public async Task Run_WhenASyncBeforeAnUpdateFails_CountsOnlyTheStepsThatRan()
    {
        // Arrange
        _steps.FailingSteps.Add("sync P1: Update message Prepare");

        // Act
        await Create(null, null, false).Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("  2 syncs before an update (Prepare): ", output, StringComparison.Ordinal);
        Assert.Contains("  1 package updates (restore, dotnet outdated): ", output, StringComparison.Ordinal);
        Assert.Contains("  1 syncs after an update (Update): ", output, StringComparison.Ordinal);
    }

    //the summary of the failures is logged with the logger of the action
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WhenGitWorkIsLoggedOrNot_GivesTheLoggerToTheActionOnlyWhenLogged(bool logGitWork)
    {
        // Arrange
        _parameters.LogGitWork = logGitWork;
        _steps.FailingSteps.Add("update P1/GitA");

        // Act
        await Create(null, "P1", false).Run();

        // Assert
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString() == "dotnet outdated failed for: GitA"),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            logGitWork ? Times.Once() : Times.Never());
#pragma warning restore CA1873
    }

    private UpdateOutdatedPackagesToolAction Create(string? projectGroupName, string? projectName,
        bool includeRemoteProjects)
    {
        return UpdateOutdatedPackagesToolAction.Create(_logger.Object, _parametersManager.Object, _steps,
            projectGroupName, projectName, includeRemoteProjects, true, (fieldName, defaultValue) =>
            {
                _inputs.Add((fieldName, defaultValue));
                return "Update message";
            }, (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            }, _timeProvider);
    }

    private void AddGit(string gitName, string gitIgnorePatternName)
    {
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{gitName}.git",
            GitProjectFolderName = gitName,
            GitIgnorePatternName = gitIgnorePatternName
        };
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
