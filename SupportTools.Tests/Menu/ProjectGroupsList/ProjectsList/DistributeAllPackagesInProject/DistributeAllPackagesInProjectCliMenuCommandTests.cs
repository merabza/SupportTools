using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.DistributeAllPackagesInProject;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportTools.Tests.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.DistributeAllPackagesInProject;

//no git, dotnet or package server runs: the steps are a fake that records the calls. The project's gits list chooses
//the packages, wherever these are registered, and only the project itself takes them at the end. Core and Util
//(it uses Core) are packages of another group; App and Other of G1 use them, Web uses Core and nobody uses Web
[Collection(ConsoleCaptureCollection.Name)]
public sealed class DistributeAllPackagesInProjectCliMenuCommandTests : IDisposable
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

    public DistributeAllPackagesInProjectCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);

        AddProject("Core", EProjectType.IsPackage, true, "Core");
        AddProject("Util", EProjectType.IsPackage, true, "Util", "Core");
        AddProject("Web", EProjectType.IsPackage, true, "Web", "Core");
        AddProject("App", EProjectType.Standard, true, "App", "Util", "Core");
        AddProject("Other", EProjectType.Standard, true, "Other", "Core");
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
        var sut = new DistributeAllPackagesInProjectCliMenuCommand(_logger.Object, "SupportTools",
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, new SupportToolsMenuParameters(), "App");

        // Assert
        Assert.Equal("Distribute all packages in project", sut.Name);
        Assert.Equal("App", sut.ParentMenuName);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_NamesTheProject()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will sync gits, check build, check solution, build and distribute packages for the package type projects whose repositories are in the gits list of project App (in any group), ordered by package dependencies, and distribute them only to this project. Packages that have not changed since their last upload are not built and uploaded again",
            result);
    }

    //Web is not in the gits list of App, and Other, which uses Core too, takes nothing
    [Fact]
    public async Task RunBody_WhenProjectIsNotAPackage_ProcessesThePackagesItUsesAndDistributesThemOnlyToIt()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Equal([
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Core"), "distribute Core to Util",
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Util"), "distribute Core, Util to App"
        ], _steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains("Package projects processing order: Core, Util" + Environment.NewLine, output,
            StringComparison.Ordinal);
        Assert.Contains("Distributing packages to project App (1/1)", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished. Package projects processed: 2, built and uploaded: 2, unchanged since the last upload: 0",
            output, StringComparison.Ordinal);
    }

    //a package project is processed itself, after the packages it uses; App, which uses it, takes nothing
    [Fact]
    public async Task RunBody_WhenProjectIsAPackage_ProcessesItAfterThePackagesItUsesAndDistributesToNoOtherProject()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("Util"));

        // Assert
        Assert.True(result);
        Assert.Equal([
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Core"), "distribute Core to Util",
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Util")
        ], _steps.Calls);
        Assert.DoesNotContain("Distributing packages to project", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the menu does not show the command for such a project; if it still runs, UseLocalPackages works as in the main
    //menu command: the project takes no packages, so its gits choose none
    [Fact]
    public async Task RunBody_WhenProjectDoesNotUseLocalPackages_ProcessesNothing()
    {
        // Arrange
        _parameters.Projects["App"].UseLocalPackages = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Empty(_steps.Calls);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a package that is not included is not processed and is not distributed, so Util is built with the Core version it
    //already has
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunBody_WhenAPackageItUsesIsRemote_AsksWhetherToIncludeIt(bool include)
    {
        // Arrange
        AddProject("Core", EProjectType.IsPackage, false, "Core");
        _answer = include;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([("Include 1 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        List<string> expectedCalls = include
            ?
            [
                .. FakePackageDistributionSteps.GetBuiltPackageCalls("Core"), "distribute Core to Util",
                .. FakePackageDistributionSteps.GetBuiltPackageCalls("Util"), "distribute Core, Util to App"
            ]
            : [.. FakePackageDistributionSteps.GetBuiltPackageCalls("Util"), "distribute Util to App"];
        Assert.Equal(expectedCalls, _steps.Calls);
    }

    //the packages are already uploaded when the project fails to take them, and the command fails
    [Fact]
    public async Task RunBody_WhenTheProjectCannotTakeThePackages_ReportsItAndFails()
    {
        // Arrange
        _steps.FailingStep = "distribute";
        _steps.FailingProject = "App";

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal("distribute Core, Util to App", _steps.Calls[^1]);
        string output = _consoleOutput.ToString();
        Assert.Contains("Package distribution failed for project App", output, StringComparison.Ordinal);
        Assert.Contains("Distribute all packages finished with errors. Packages were not distributed to projects: App",
            output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenProjectIsNotFound_ReportsItAndFails()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("Missing"));

        // Assert
        Assert.False(result);
        Assert.Empty(_steps.Calls);
        Assert.Contains("Project Missing does not found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateMenuCommand_WhenCalled_CreatesTheCommandOfTheCurrentProject()
    {
        // Arrange
        var app = new Mock<IApplication>();
        app.SetupGet(x => x.AppName).Returns("SupportTools");
        var sut = new DistributeAllPackagesInProjectCliMenuCommandFactoryStrategy(app.Object,
            new Mock<ILogger<DistributeAllPackagesInProjectCliMenuCommandFactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new SupportToolsMenuParameters { ProjectName = "Util" },
            _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<DistributeAllPackagesInProjectCliMenuCommand>(command);
        Assert.Equal("Util", command.ParentMenuName);
        Assert.Contains("of project Util (in any group)", await CliMenuTestAccess.InvokeGetActionDescription(command),
            StringComparison.Ordinal);
    }

    //strategies are looked up by type name, so the project submenu shows the command only if its name is listed; it
    //comes before Reverse Package Distribution In Project, as in the main menu
    [Fact]
    public void ProjectSubMenuCommandFactoryStrategyNames_ListTheStrategyRightAfterPackageDistribution()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(DistributeAllPackagesInProjectCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(PackageDistributionCliMenuCommandFactoryStrategy), strategyNames[index - 1]);
    }

    private DistributeAllPackagesInProjectCliMenuCommand CreateSut(string projectName = "App")
    {
        return new DistributeAllPackagesInProjectCliMenuCommand(_logger.Object, _parametersManager.Object, projectName,
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
            ProjectGroupName = projectType == EProjectType.IsPackage ? "Packages" : "G1",
            UseLocalPackages = true,
            ProjectType = projectType,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing"),
            GitProjectNames = [.. gitNames]
        };
    }
}
