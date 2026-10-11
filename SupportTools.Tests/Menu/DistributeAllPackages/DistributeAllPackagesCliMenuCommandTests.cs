using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.DistributeAllPackages;

//no git, dotnet or package server runs: the steps are a fake that records the calls and fails where a test says.
//The command decides the order, the messages and where the run stops
[Collection(ConsoleCaptureCollection.Name)]
public sealed class DistributeAllPackagesCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly FakePackageDistributionSteps _steps = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public DistributeAllPackagesCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheMenu()
    {
        // Act
        var sut = new DistributeAllPackagesCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, new SupportToolsMenuParameters());

        // Assert
        Assert.Equal("Distribute all packages", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_DescribesTheWholeProcess()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will sync gits, check build, check solution, build and distribute packages for all package type projects, ordered by package dependencies",
            result);
    }

    [Fact]
    public async Task RunBody_WhenThereAreNoPackageProjects_WarnsAndSucceeds()
    {
        // Arrange
        AddProject("App", EProjectType.Standard, true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Empty(_steps.Calls);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenOnlyRemotePackagesExistAndAreNotIncluded_WarnsAndSucceeds()
    {
        // Arrange
        AddProject("RemotePackage", EProjectType.IsPackage, false);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([("Include 1 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        Assert.Empty(_steps.Calls);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemotePackagesAreIncluded_ProcessesThemToo()
    {
        // Arrange
        AddProject("RemotePackage", EProjectType.IsPackage, false);
        _answer = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("Package projects processing order: RemotePackage", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Contains("distribute RemotePackage", _steps.Calls);
    }

    //every step of a package runs in this order, and the next package comes only after the last step of the previous
    [Fact]
    public async Task RunBody_WhenEveryStepSucceeds_RunsAllStepsOfEveryPackageAndReportsTheCount()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);
        _logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(
        [
            "sync Alpha", "build Alpha noIncremental=False", "solution Alpha", "package Alpha", "distribute Alpha",
            "final sync Alpha", "sync Beta", "build Beta noIncremental=False", "solution Beta", "package Beta",
            "distribute Beta", "final sync Beta"
        ], _steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains("Processing package project Alpha (1/2)", output, StringComparison.Ordinal);
        Assert.Contains("Processing package project Beta (2/2)", output, StringComparison.Ordinal);
        Assert.Contains("Distribute all packages finished. Package projects processed: 2", output,
            StringComparison.Ordinal);
        VerifyFinishedLogged(Times.Once());
    }

    [Fact]
    public async Task RunBody_WhenInformationIsNotLogged_LogsNoSummary()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        VerifyFinishedLogged(Times.Never());
    }

    //a package built with warnings is still distributed
    [Fact]
    public async Task RunBody_WhenBuildHasWarnings_GoesOn()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        _steps.BuildResult = new ProjectBuildCheckResult(EProjectBuildCheckStatus.SuccessWithWarnings, 0, 3);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("final sync Alpha", _steps.Calls);
    }

    //the first step that fails stops the whole run, with its own message
    [Theory]
    [InlineData("sync", "Sync gits failed for project Alpha", 1)]
    [InlineData("build", "Build check failed for project Alpha with status BuildFailed, errors: 2", 2)]
    [InlineData("solution", "Package solution check failed for project Alpha", 3)]
    [InlineData("package", "Build package failed for project Alpha", 4)]
    [InlineData("distribute", "Package distribution failed for project Alpha", 5)]
    [InlineData("final sync", "Final sync gits failed for project Alpha", 6)]
    public async Task RunBody_WhenAStepFails_ReportsItAndStopsTheRun(string failingStep, string expectedMessage,
        int expectedCallCount)
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);
        _steps.FailingStep = failingStep;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(expectedCallCount, _steps.Calls.Count);
        string output = _consoleOutput.ToString();
        Assert.Contains(expectedMessage, output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages stopped on project Alpha. Successfully processed 0 of 2 package projects", output,
            StringComparison.Ordinal);
        Assert.DoesNotContain("Processing package project Beta", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Distribute all packages finished", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenBuildCheckGivesNoResult_ReportsItAsNotChecked()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        _steps.BuildResult = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("Build check failed for project Alpha with status NotChecked", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenALaterPackageFails_CountsTheProcessedOnes()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);
        _steps.FailingStep = "package";
        _steps.FailingProject = "Beta";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains(
            "Distribute all packages stopped on project Beta. Successfully processed 1 of 2 package projects",
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a package depends on the packages whose repositories are in its gits list; the ready ones go in name order
    [Fact]
    public async Task RunBody_WhenPackagesDependOnEachOther_OrdersThemByTheirDependencies()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Alpha", "Core");
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Beta", EProjectType.IsPackage, true, "Beta", "Tool");
        AddProject("Tool", EProjectType.Standard, true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Package projects processing order: Beta, Core, Alpha" + Environment.NewLine,
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a repeated git counts once, so the package does not wait for a second, never coming, dependency
    [Fact]
    public async Task RunBody_WhenADependencyIsListedTwice_CountsItOnce()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Core", "Core");
        AddProject("Core", EProjectType.IsPackage, true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("Package projects processing order: Core, Alpha" + Environment.NewLine,
            _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenPackagesDependOnEachOtherInACycle_ReportsTheCycleAndFails()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Beta");
        AddProject("Beta", EProjectType.IsPackage, true, "Alpha");
        AddProject("Gamma", EProjectType.IsPackage, true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains(
            "Cannot order package projects by dependencies, cycle detected. Projects in cycle: Alpha, Beta" +
            Environment.NewLine, output, StringComparison.Ordinal);
        Assert.DoesNotContain("Package projects processing order", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenCancelled_StopsBeforeTheFirstPackage()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CliMenuTestAccess.InvokeRunBody(CreateSut(), cancellationTokenSource.Token));

        // Assert
        Assert.Empty(_steps.Calls);
        Assert.DoesNotContain("Processing package project Alpha", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private DistributeAllPackagesCliMenuCommand CreateSut()
    {
        return new DistributeAllPackagesCliMenuCommand(_logger.Object, _parametersManager.Object,
            (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            }, _steps);
    }

    private void AddProject(string projectName, EProjectType projectType, bool isLocal, params string[] gitNames)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectType = projectType,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing"),
            GitProjectNames = [.. gitNames]
        };
    }

    private void VerifyFinishedLogged(Times times)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) =>
                value.ToString() == "Distribute all packages finished. Package projects processed: 2" ||
                value.ToString() == "Distribute all packages finished. Package projects processed: 1"),
            It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
#pragma warning restore CA1873
    }

    //records every step; a step named FailingStep fails for FailingProject (or for every project when it is null).
    //The first sync of a project is "sync", the second one "final sync"
    private sealed class FakePackageDistributionSteps : IPackageDistributionSteps
    {
        private readonly HashSet<string> _syncedProjectNames = [];

        public List<string> Calls { get; } = [];
        public string? FailingStep { get; set; }
        public string? FailingProject { get; set; }
        public ProjectBuildCheckResult? BuildResult { get; set; } = new(EProjectBuildCheckStatus.Success);

        public Task<bool> SyncProjectGits(string projectName, CancellationToken cancellationToken)
        {
            string step = _syncedProjectNames.Add(projectName) ? "sync" : "final sync";
            return Task.FromResult(Run(step, projectName));
        }

        public ProjectBuildCheckResult? CheckBuild(string projectName, ProjectModel project, bool noIncremental,
            CancellationToken cancellationToken)
        {
            Calls.Add($"build {projectName} noIncremental={noIncremental}");
            return Fails("build", projectName)
                ? new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 2, 0)
                : BuildResult;
        }

        public bool CheckPackageSolution(string projectName)
        {
            return Run("solution", projectName);
        }

        public bool BuildAndUploadPackage(string projectName)
        {
            return Run("package", projectName);
        }

        public Task<bool> DistributePackage(string projectName, CancellationToken cancellationToken)
        {
            return Task.FromResult(Run("distribute", projectName));
        }

        private bool Run(string step, string projectName)
        {
            Calls.Add($"{step} {projectName}");
            return !Fails(step, projectName);
        }

        private bool Fails(string step, string projectName)
        {
            return FailingStep == step && (FailingProject is null || FailingProject == projectName);
        }
    }
}
