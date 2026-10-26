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
            "This process will sync gits, check build, check solution, build and distribute packages for all package type projects, ordered by package dependencies. Packages that have not changed since their last upload are not built and uploaded again",
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
        Assert.Contains("package RemotePackage", _steps.Calls);
    }

    [Fact]
    public async Task RunBody_WhenAPackageDoesNotUseLocalPackages_SkipsIt()
    {
        // Arrange
        AddProject("Package", EProjectType.IsPackage, true);
        AddProject("OtherPackage", EProjectType.IsPackage, true);
        _parameters.Projects["OtherPackage"].UseLocalPackages = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("Package projects processing order: Package" + Environment.NewLine, _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.DoesNotContain(_steps.Calls, x => x.Contains("OtherPackage", StringComparison.Ordinal));
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
        Assert.Equal([
            "sync Alpha", "check Alpha", "build Alpha noIncremental=False", "solution Alpha", "package Alpha",
            "final sync Alpha", "sync Beta", "check Beta", "build Beta noIncremental=False", "solution Beta",
            "package Beta", "final sync Beta"
        ], _steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains("Processing package project Alpha (1/2)", output, StringComparison.Ordinal);
        Assert.Contains("Processing package project Beta (2/2)", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished. Package projects processed: 2, built and uploaded: 2, unchanged since the last upload: 0",
            output, StringComparison.Ordinal);
        VerifyFinishedLogged(Times.Once(),
            "Distribute all packages finished. Package projects processed: 2, built and uploaded: 2, unchanged since the last upload: 0");
    }

    //a package that has not changed since its last upload gets only its solution check after the first sync: the
    //packages on the server stay the newest ones
    [Fact]
    public async Task RunBody_WhenAPackageHasNotChangedSinceItsLastUpload_SkipsItsBuildCheckBuildAndUpload()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);
        _steps.UnchangedVersions["Alpha"] = "1.0.5";
        _logger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([
            "sync Alpha", "check Alpha", "solution Alpha", "sync Beta", "check Beta",
            "build Beta noIncremental=False", "solution Beta", "package Beta", "final sync Beta"
        ], _steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains(
            "Project Alpha has not changed since its packages 1.0.5 were uploaded, build check, build and upload are skipped",
            output, StringComparison.Ordinal);
        Assert.DoesNotContain("Project Beta has not changed", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished. Package projects processed: 2, built and uploaded: 1, unchanged since the last upload: 1",
            output, StringComparison.Ordinal);
        VerifyFinishedLogged(Times.Once(),
            "Distribute all packages finished. Package projects processed: 2, built and uploaded: 1, unchanged since the last upload: 1");
    }

    //the projects that use an unchanged package still take it: a run that stopped earlier may have uploaded it without
    //distributing it, and a project that already has its newest version does not change
    [Fact]
    public async Task RunBody_WhenAPackageHasNotChanged_StillDistributesItToTheProjectsThatUseIt()
    {
        // Arrange
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Alpha", EProjectType.IsPackage, true, "Alpha", "Core");
        AddProject("App", EProjectType.Standard, true, "App", "Core");
        _steps.UnchangedVersions["Core"] = "1.0.5";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([
            "sync Core", "check Core", "solution Core", "distribute Core to Alpha", "sync Alpha", "check Alpha",
            "build Alpha noIncremental=False", "solution Alpha", "package Alpha", "final sync Alpha",
            "distribute Core to App"
        ], _steps.Calls);
        Assert.Equal("Distribute all packages: use new Core packages in Alpha", _steps.CommitMessages[1]);
    }

    //the solution check of an unchanged package fails like the one of a package that is built
    [Fact]
    public async Task RunBody_WhenTheSolutionCheckOfAnUnchangedPackageFails_SkipsItAndFails()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);
        _steps.UnchangedVersions["Alpha"] = "1.0.5";
        _steps.FailingStep = "solution";
        _steps.FailingProject = "Alpha";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(["sync Alpha", "check Alpha", "solution Alpha"], _steps.Calls[..3]);
        Assert.Equal("final sync Beta", _steps.Calls[^1]);
        string output = _consoleOutput.ToString();
        Assert.Contains("Package solution check failed for project Alpha", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished with errors. Successfully processed 1 of 2 package projects. Failed: Alpha",
            output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenInformationIsNotLogged_LogsNoSummary()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        VerifyFinishedLogged(Times.Never(),
            "Distribute all packages finished. Package projects processed: 1, built and uploaded: 1, unchanged since the last upload: 0");
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

    //the first step that fails stops that package, with its own message; the other packages are processed and the run
    //fails at the end
    [Theory]
    [InlineData("sync", "Sync gits failed for project Alpha", 1)]
    [InlineData("build", "Build check failed for project Alpha with status BuildFailed, errors: 2", 3)]
    [InlineData("solution", "Package solution check failed for project Alpha", 4)]
    [InlineData("package", "Build package failed for project Alpha", 5)]
    [InlineData("final sync", "Final sync gits failed for project Alpha", 6)]
    public async Task RunBody_WhenAStepFails_ReportsItSkipsThePackageAndGoesOn(string failingStep,
        string expectedMessage, int expectedCallCount)
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true);
        AddProject("Beta", EProjectType.IsPackage, true);
        _steps.FailingStep = failingStep;
        _steps.FailingProject = "Alpha";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(expectedCallCount,
            _steps.Calls.FindAll(x => x.Contains(" Alpha", StringComparison.Ordinal)).Count);
        Assert.Equal("final sync Beta", _steps.Calls[^1]);
        string output = _consoleOutput.ToString();
        Assert.Contains(expectedMessage, output, StringComparison.Ordinal);
        Assert.Contains(
            "Package project Alpha failed and is skipped, distribution goes on with the other package projects", output,
            StringComparison.Ordinal);
        Assert.Contains("Processing package project Beta (2/2)", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished with errors. Successfully processed 1 of 2 package projects. Failed: Alpha" +
            Environment.NewLine, output, StringComparison.Ordinal);
        Assert.DoesNotContain("Distribute all packages finished. Package projects processed", output,
            StringComparison.Ordinal);
    }

    //a package that uses a failed package, directly or through a skipped one, is skipped and is not built with its old
    //version; the other packages and the projects that use them go on
    [Fact]
    public async Task RunBody_WhenAPackageFails_SkipsThePackagesThatUseItAndGoesOnWithTheOthers()
    {
        // Arrange
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Util", EProjectType.IsPackage, true, "Util", "Core");
        AddProject("Web", EProjectType.IsPackage, true, "Web", "Util");
        AddProject("Other", EProjectType.IsPackage, true, "Other");
        AddProject("App", EProjectType.Standard, true, "App", "Core", "Other");
        _steps.FailingStep = "build";
        _steps.FailingProject = "Core";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([
            "sync Core", "check Core", "build Core noIncremental=False", "sync Other", "check Other",
            "build Other noIncremental=False", "solution Other", "package Other", "final sync Other",
            "distribute Other to App"
        ], _steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains(
            "Package project Util is skipped, because it uses package projects that were not distributed: Core", output,
            StringComparison.Ordinal);
        Assert.Contains(
            "Package project Web is skipped, because it uses package projects that were not distributed: Util", output,
            StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished with errors. Successfully processed 1 of 4 package projects. Failed: Core. Skipped because they use failed packages: Util, Web",
            output, StringComparison.Ordinal);
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
            "Distribute all packages finished with errors. Successfully processed 1 of 2 package projects. Failed: Beta",
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

    //the first sync names the packages of this run that the project uses, so nothing asks for a commit message
    [Fact]
    public async Task RunBody_WhenPackagesAreSynced_CommitsWithMessagesThatNameTheDistributedPackages()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Alpha", "Core", "Util");
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Util", EProjectType.IsPackage, true, "Util");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal([
            "Distribute all packages: before Core package", "Distribute all packages: after Core package",
            "Distribute all packages: before Util package", "Distribute all packages: after Util package",
            "Distribute all packages: use new Core, Util packages in Alpha",
            "Distribute all packages: after Alpha package"
        ], _steps.CommitMessages);
    }

    //a package takes the packages of this run that it uses right before its first sync, in the processing order, when
    //all of them are already pushed; a git that is not a package of this run is not distributed
    [Fact]
    public async Task RunBody_WhenAPackageUsesPackagesOfThisRun_DistributesThemToItBeforeItsFirstSync()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Alpha", "Util", "Core", "Tool");
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Util", EProjectType.IsPackage, true, "Util", "Core");
        AddProject("Tool", EProjectType.Standard, true, "Tool");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([
            "sync Core", "check Core", "build Core noIncremental=False", "solution Core", "package Core",
            "final sync Core", "distribute Core to Util", "sync Util", "check Util", "build Util noIncremental=False",
            "solution Util", "package Util", "final sync Util", "distribute Core, Util to Alpha", "sync Alpha",
            "check Alpha", "build Alpha noIncremental=False", "solution Alpha", "package Alpha", "final sync Alpha"
        ], _steps.Calls);
    }

    //the package is not synced, built or uploaded with the old versions of the packages it uses
    [Fact]
    public async Task RunBody_WhenDistributionToAPackageFails_SkipsItBeforeItsSync()
    {
        // Arrange
        AddProject("Alpha", EProjectType.IsPackage, true, "Alpha", "Core");
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        _steps.FailingStep = "distribute";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal("distribute Core to Alpha", _steps.Calls[^1]);
        string output = _consoleOutput.ToString();
        Assert.Contains("Package distribution failed for project Alpha", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished with errors. Successfully processed 1 of 2 package projects. Failed: Alpha",
            output, StringComparison.Ordinal);
    }

    //the projects that are not packages take the packages of this run that they use after the last package, in name
    //order; the ones with UseLocalPackages off, the ones that use none of them and the package projects that were not
    //processed take nothing
    [Fact]
    public async Task RunBody_WhenAllPackagesAreProcessed_DistributesThemToTheOtherProjectsAtTheEnd()
    {
        // Arrange
        AddProject("Util", EProjectType.IsPackage, true, "Util", "Core");
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Web", EProjectType.IsService, true, "Web", "Util");
        AddProject("App", EProjectType.Standard, true, "App", "Util", "Core", "Tool");
        AddProject("Tool", EProjectType.Standard, true, "Tool");
        AddProject("Off", EProjectType.Standard, true, "Off", "Core");
        _parameters.Projects["Off"].UseLocalPackages = false;
        AddProject("OffPackage", EProjectType.IsPackage, true, "OffPackage", "Core");
        _parameters.Projects["OffPackage"].UseLocalPackages = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(["distribute Core to Util", "distribute Core, Util to App", "distribute Util to Web"],
            _steps.Calls.FindAll(x => x.StartsWith("distribute", StringComparison.Ordinal)));
        Assert.Equal("distribute Util to Web", _steps.Calls[^1]);
        string output = _consoleOutput.ToString();
        Assert.Contains("Distributing packages to project App (1/2)", output, StringComparison.Ordinal);
        Assert.Contains("Distributing packages to project Web (2/2)", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished. Package projects processed: 2, built and uploaded: 2, unchanged since the last upload: 0",
            output, StringComparison.Ordinal);
    }

    //all the packages are already uploaded, so one failed project does not stop the others, but the run fails
    [Fact]
    public async Task RunBody_WhenDistributionToAnotherProjectFails_GoesOnWithTheOthersAndFails()
    {
        // Arrange
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("App", EProjectType.Standard, true, "App", "Core");
        AddProject("Web", EProjectType.IsService, true, "Web", "Core");
        _steps.FailingStep = "distribute";
        _steps.FailingProject = "App";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(["distribute Core to App", "distribute Core to Web"], _steps.Calls[^2..]);
        string output = _consoleOutput.ToString();
        Assert.Contains("Package distribution failed for project App", output, StringComparison.Ordinal);
        Assert.Contains("Distribute all packages finished with errors. Packages were not distributed to projects: App",
            output, StringComparison.Ordinal);
        Assert.DoesNotContain("Distribute all packages finished. Package projects processed", output,
            StringComparison.Ordinal);
    }

    //a failed package is distributed to none of the projects that are not packages
    [Fact]
    public async Task RunBody_WhenAPackageFails_DistributesNothingToTheOtherProjects()
    {
        // Arrange
        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("App", EProjectType.Standard, true, "App", "Core");
        _steps.FailingStep = "package";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.DoesNotContain(_steps.Calls, x => x.StartsWith("distribute", StringComparison.Ordinal));
        Assert.DoesNotContain("Distributing packages to project", _consoleOutput.ToString(), StringComparison.Ordinal);
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
            UseLocalPackages = true,
            ProjectType = projectType,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing"),
            GitProjectNames = [.. gitNames]
        };
    }

    private void VerifyFinishedLogged(Times times, string message)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) => value.ToString() == message), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
#pragma warning restore CA1873
    }

    //records every step; a step named FailingStep fails for FailingProject (or for every project when it is null).
    //The first sync of a project is "sync", the second one "final sync"; a distribution is "distribute" for the project
    //that takes the packages; "check" asks whether a package has changed since its last upload, which only the projects
    //in UnchangedVersions have not
    private sealed class FakePackageDistributionSteps : IPackageDistributionSteps
    {
        private readonly HashSet<string> _syncedProjectNames = [];

        public List<string> Calls { get; } = [];
        public List<string> CommitMessages { get; } = [];
        public string? FailingStep { get; set; }
        public string? FailingProject { get; set; }
        public ProjectBuildCheckResult? BuildResult { get; set; } = new(EProjectBuildCheckStatus.Success);
        public Dictionary<string, string> UnchangedVersions { get; } = [];

        public Task<bool> SyncProjectGits(string projectName, string commitMessage, CancellationToken cancellationToken)
        {
            CommitMessages.Add(commitMessage);
            string step = _syncedProjectNames.Add(projectName) ? "sync" : "final sync";
            return Task.FromResult(Run(step, projectName));
        }

        public Task<string?> GetUnchangedUploadedPackageVersion(string projectName, CancellationToken cancellationToken)
        {
            Calls.Add($"check {projectName}");
            return Task.FromResult(UnchangedVersions.GetValueOrDefault(projectName));
        }

        public ProjectBuildCheckResult? CheckBuild(string projectName, ProjectModel project, bool noIncremental,
            CancellationToken cancellationToken)
        {
            Calls.Add($"build {projectName} noIncremental={noIncremental}");
            return Fails("build", projectName)
                ? new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 2)
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

        public Task<bool> DistributePackagesToProject(string projectName, IReadOnlyList<string> packageProjectNames,
            CancellationToken cancellationToken)
        {
            Calls.Add($"distribute {string.Join(", ", packageProjectNames)} to {projectName}");
            return Task.FromResult(!Fails("distribute", projectName));
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
