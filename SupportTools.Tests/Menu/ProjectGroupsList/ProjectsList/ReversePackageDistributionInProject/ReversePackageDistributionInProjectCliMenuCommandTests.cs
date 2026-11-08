using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ReversePackageDistributionInProject;
using SupportTools.Tests.Menu.ReversePackageDistribution;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.ReversePackageDistributionInProject;

//no dotnet runs: the csproj changes go to a fake editor. App and Another of group G1 are local projects that use the
//own package Lib.A
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ReversePackageDistributionInProjectCliMenuCommandTests : IDisposable
{
    private readonly string _appCsproj;
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly FakeProjectReferencesEditor _editor = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ReversePackageDistributionInProjectCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        ReversePackageDistributionTestProjects.AddOwnPackageLibA(_parameters);
        _appCsproj = ReversePackageDistributionTestProjects.AddProjectWithOwnPackage(_parameters, _tempFolder, "App",
            "G1");
        ReversePackageDistributionTestProjects.AddProjectWithOwnPackage(_parameters, _tempFolder, "Another", "G1");
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheProjectMenu()
    {
        // Act
        var sut = new ReversePackageDistributionInProjectCliMenuCommand(_logger.Object, _parametersManager.Object,
            "App");

        // Assert
        Assert.Equal("Reverse Package Distribution In Project", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.Equal("App", sut.ParentMenuName);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_NamesTheProject()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will replace PackageReferences of own packages with ProjectReferences in the main repository of project App",
            result);
    }

    //Another of the same group uses Lib.A too, but only App changes
    [Fact]
    public async Task RunBody_WhenCalled_ReplacesTheOwnPackagesOfThisProjectOnly()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(ReversePackageDistributionTestProjects.GetReplaceCalls(_appCsproj, _tempFolder, "App"),
            _editor.Calls);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 1");
    }

    [Fact]
    public async Task RunBody_WhenProjectIsNotRegistered_ReportsItAndFails()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("Missing"));

        // Assert
        Assert.False(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains("Project Missing does not found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenProjectDoesNotUseLocalPackages_SkipsIt()
    {
        // Arrange
        _parameters.Projects["App"].UseLocalPackages = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        Assert.Contains("UseLocalPackages is off for project App, skipped", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 0");
    }

    //a single project is not counted as a skipped remote-only one: its missing main repository is reported
    [Fact]
    public async Task RunBody_WhenProjectIsRemote_ReportsTheMissingMainRepository()
    {
        // Arrange
        string missingFolder = Path.Combine(_tempFolder, "Missing");
        _parameters.Projects["App"] = new ProjectModel
        {
            UseLocalPackages = true, ProjectFolderName = missingFolder, GitProjectNames = ["App"]
        };

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains(
            $"Main repository folder {Path.Combine(missingFolder, "App")} does not exists for project App, skipped",
            output, StringComparison.Ordinal);
        Assert.DoesNotContain("remote-only", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenReferenceCannotBeAdded_RestoresThePackageAndFails()
    {
        // Arrange
        _editor.FailingReference = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([
            .. ReversePackageDistributionTestProjects.GetReplaceCalls(_appCsproj, _tempFolder, "App"),
            $"package {_appCsproj} Lib.A "
        ], _editor.Calls);
    }

    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheCommandOfTheCurrentProject()
    {
        // Arrange
        var sut = new ReversePackageDistributionInProjectCliMenuCommandFactoryStrategy(
            new Mock<ILogger<ReversePackageDistributionInProjectCliMenuCommandFactoryStrategy>>().Object,
            new SupportToolsMenuParameters { ProjectName = "App" }, _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        var reverseCommand = Assert.IsType<ReversePackageDistributionInProjectCliMenuCommand>(command);
        Assert.Equal("App", reverseCommand.ParentMenuName);
    }

    //strategies are looked up by type name, so the project submenu shows the command only if its name is listed
    [Fact]
    public void ProjectSubMenuCommandFactoryStrategyNames_ListTheStrategyRightAfterPackageDistribution()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(ReversePackageDistributionInProjectCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(PackageDistributionCliMenuCommandFactoryStrategy), strategyNames[index - 1]);
    }

    private ReversePackageDistributionInProjectCliMenuCommand CreateSut(string projectName = "App")
    {
        return new ReversePackageDistributionInProjectCliMenuCommand(_logger.Object, _parametersManager.Object,
            projectName, () => _editor);
    }

    private void VerifyLogged(LogLevel logLevel, string message)
    {
        //a Moq expression: the arguments only describe the expected call, nothing is evaluated for logging
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(logLevel, It.IsAny<EventId>(), It.Is<It.IsAnyType>((value, _) => value!.ToString() == message),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }
}
