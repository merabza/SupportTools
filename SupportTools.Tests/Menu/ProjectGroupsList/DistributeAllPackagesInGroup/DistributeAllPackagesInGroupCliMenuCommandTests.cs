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
using SupportTools.Menu.ProjectGroupsList.DistributeAllPackagesInGroup;
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;
using SupportTools.Tests.Menu.DistributeAllPackages;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.DistributeAllPackagesInGroup;

//no git, dotnet or package server runs: the steps are a fake that records the calls. The gits of the group's projects
//that use local packages choose the packages, wherever these are registered, and only the group's projects that are
//not packages take them at the end
[Collection(ConsoleCaptureCollection.Name)]
public sealed class DistributeAllPackagesInGroupCliMenuCommandTests : IDisposable
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

    public DistributeAllPackagesInGroupCliMenuCommandTests()
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
        var sut = new DistributeAllPackagesInGroupCliMenuCommand(_logger.Object, "SupportTools",
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, new SupportToolsMenuParameters(), "G1");

        // Assert
        Assert.Equal("Distribute all packages in group projects", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task GetActionDescription_WhenCalled_NamesTheGroup()
    {
        // Act
        string? result = await CliMenuTestAccess.InvokeGetActionDescription(CreateSut());

        // Assert
        Assert.Equal(
            "This process will sync gits, check build, check solution, build and distribute packages for the package type projects whose repositories are in the gits lists of the projects of group G1 (in any group), ordered by package dependencies, and distribute them only to the projects of this group. Packages that have not changed since their last upload are not built and uploaded again",
            result);
    }

    //Core and Util are registered in another group, but the projects of G1 name them; Lib is a package of G1. Web is
    //named only by NoLocal, which does not use local packages, and by Other of G2; Off does not use local packages
    //itself. Only App and Service of G1 take the packages, Other of G2 does not
    [Fact]
    public async Task RunBody_WhenCalled_ProcessesThePackagesTheGroupUsesAndDistributesThemOnlyToTheGroup()
    {
        // Arrange
        AddProject("Core", "Packages", EProjectType.IsPackage, true, "Core");
        AddProject("Util", "Packages", EProjectType.IsPackage, true, "Util", "Core");
        AddProject("Web", "Packages", EProjectType.IsPackage, true, "Web", "Core");
        AddProject("Off", "Packages", EProjectType.IsPackage, true, "Off");
        _parameters.Projects["Off"].UseLocalPackages = false;
        AddProject("Lib", "G1", EProjectType.IsPackage, true, "Lib", "Util");
        AddProject("App", "G1", EProjectType.Standard, true, "App", "Lib", "Util", "Core", "Off");
        AddProject("Service", "G1", EProjectType.IsService, true, "Service", "Core");
        AddProject("NoLocal", "G1", EProjectType.Standard, true, "NoLocal", "Web");
        _parameters.Projects["NoLocal"].UseLocalPackages = false;
        AddProject("Other", "G2", EProjectType.Standard, true, "Other", "Core", "Web");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Equal([
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Core"), "distribute Core to Util",
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Util"), "distribute Util to Lib",
            .. FakePackageDistributionSteps.GetBuiltPackageCalls("Lib"), "distribute Core, Util, Lib to App",
            "distribute Core to Service"
        ], _steps.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains("Package projects processing order: Core, Util, Lib" + Environment.NewLine, output,
            StringComparison.Ordinal);
        Assert.Contains("Distributing packages to project App (1/2)", output, StringComparison.Ordinal);
        Assert.Contains("Distributing packages to project Service (2/2)", output, StringComparison.Ordinal);
        Assert.Contains(
            "Distribute all packages finished. Package projects processed: 3, built and uploaded: 3, unchanged since the last upload: 0",
            output, StringComparison.Ordinal);
    }

    //a project that does not use local packages takes no packages, so its gits choose none (UseLocalPackages works
    //as in the main menu command)
    [Fact]
    public async Task RunBody_WhenOnlyAProjectThatDoesNotUseLocalPackagesNamesPackages_ProcessesNothing()
    {
        // Arrange
        AddProject("Core", "Packages", EProjectType.IsPackage, true, "Core");
        AddProject("NoLocal", "G1", EProjectType.Standard, true, "NoLocal", "Core");
        _parameters.Projects["NoLocal"].UseLocalPackages = false;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_questions);
        Assert.Empty(_steps.Calls);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenTheGroupUsesNoPackages_WarnsAndSucceeds()
    {
        // Arrange
        AddProject("Core", "Packages", EProjectType.IsPackage, true, "Core");
        AddProject("App", "G1", EProjectType.Standard, true, "App");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Empty(_steps.Calls);
        Assert.Contains("No package type projects found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //a package of the group belongs to it even when its gits list does not name its own repository
    [Fact]
    public async Task RunBody_WhenAPackageOfTheGroupDoesNotNameItsOwnRepository_StillProcessesIt()
    {
        // Arrange
        AddProject("Lib", "G1", EProjectType.IsPackage, true);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(FakePackageDistributionSteps.GetBuiltPackageCalls("Lib"), _steps.Calls);
    }

    //only the remote packages that the group uses are counted; a package that is not included is not processed and is
    //not distributed, so the packages that use it are built with the version they already have
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunBody_WhenAPackageTheGroupUsesIsRemote_AsksWhetherToIncludeIt(bool include)
    {
        // Arrange
        AddProject("Core", "Packages", EProjectType.IsPackage, false, "Core");
        AddProject("RemoteOther", "Packages", EProjectType.IsPackage, false, "RemoteOther");
        AddProject("Util", "Packages", EProjectType.IsPackage, true, "Util", "Core");
        AddProject("App", "G1", EProjectType.Standard, true, "App", "Util", "Core");
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

    //a project without a group belongs to the "__No Group__" group
    [Fact]
    public async Task RunBody_WhenGroupIsTheNoGroupName_UsesTheProjectsWithoutAGroup()
    {
        // Arrange
        AddProject("Core", "Packages", EProjectType.IsPackage, true, "Core");
        AddProject("Util", "Packages", EProjectType.IsPackage, true, "Util");
        AddProject("NoGroup", null, EProjectType.Standard, true, "NoGroup", "Core");
        AddProject("App", "G1", EProjectType.Standard, true, "App", "Util");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(SupportToolsParameters.FixProjectGroupName(null)));

        // Assert
        Assert.True(result);
        Assert.Equal([.. FakePackageDistributionSteps.GetBuiltPackageCalls("Core"), "distribute Core to NoGroup"],
            _steps.Calls);
    }

    [Fact]
    public async Task CreateMenuCommand_WhenCalled_CreatesTheCommandOfTheCurrentGroup()
    {
        // Arrange
        var app = new Mock<IApplication>();
        app.SetupGet(x => x.AppName).Returns("SupportTools");
        var sut = new DistributeAllPackagesInGroupCliMenuCommandFactoryStrategy(app.Object,
            new Mock<ILogger<DistributeAllPackagesInGroupCliMenuCommandFactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new SupportToolsMenuParameters { ProjectGroupName = "G2" },
            _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<DistributeAllPackagesInGroupCliMenuCommand>(command);
        Assert.Contains("of the projects of group G2 (in any group)",
            await CliMenuTestAccess.InvokeGetActionDescription(command), StringComparison.Ordinal);
    }

    //strategies are looked up by type name, so the group submenu shows the command only if its name is listed; it
    //comes before Reverse Package Distribution, as in the main menu
    [Fact]
    public void ProjectGroupSubMenuCommandFactoryStrategyNames_ListTheStrategyRightAfterTheV3Sync()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectGroupSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(DistributeAllPackagesInGroupCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy), strategyNames[index - 1]);
    }

    private DistributeAllPackagesInGroupCliMenuCommand CreateSut(string projectGroupName = "G1")
    {
        return new DistributeAllPackagesInGroupCliMenuCommand(_logger.Object, _parametersManager.Object,
            projectGroupName, (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            }, _steps);
    }

    private void AddProject(string projectName, string? projectGroupName, EProjectType projectType, bool isLocal,
        params string[] gitNames)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = projectGroupName,
            UseLocalPackages = true,
            ProjectType = projectType,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing"),
            GitProjectNames = [.. gitNames]
        };
    }
}
