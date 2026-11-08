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
using SupportTools.Menu.ProjectGroupsList.ReversePackageDistributionInGroup;
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;
using SupportTools.Tests.Menu.ReversePackageDistribution;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ReversePackageDistributionInGroup;

//no dotnet runs: the csproj changes go to a fake editor. G1: App and Another (local, they use the own package Lib.A),
//NoLocal (local, UseLocalPackages off) and Remote (its folder is not on this computer); G2: Other (local, uses Lib.A)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ReversePackageDistributionInGroupCliMenuCommandTests : IDisposable
{
    private readonly string _anotherCsproj;
    private readonly string _appCsproj;
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly FakeProjectReferencesEditor _editor = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly string _otherCsproj;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ReversePackageDistributionInGroupCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        ReversePackageDistributionTestProjects.AddOwnPackageLibA(_parameters);
        _appCsproj = AddProject("App", "G1");
        _anotherCsproj = AddProject("Another", "G1");
        AddProject("NoLocal", "G1");
        _parameters.Projects["NoLocal"].UseLocalPackages = false;
        _parameters.Projects["Remote"] = new ProjectModel
        {
            UseLocalPackages = true,
            ProjectGroupName = "G1",
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"),
            GitProjectNames = ["Remote"]
        };
        _otherCsproj = AddProject("Other", "G2");
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
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheMenu()
    {
        // Act
        var sut = new ReversePackageDistributionInGroupCliMenuCommand(_logger.Object, _parametersManager.Object, "G1");

        // Assert
        Assert.Equal("Reverse Package Distribution In Group Projects", sut.Name);
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
            "This process will replace PackageReferences of own packages with ProjectReferences in main repositories of the projects of group G1",
            result);
    }

    //only the local projects of the group that use local packages change, in name order; Other of G2 stays as it is
    [Fact]
    public async Task RunBody_WhenCalled_ReplacesTheOwnPackagesOfTheGroupProjectsOnly()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([
            .. ReversePackageDistributionTestProjects.GetReplaceCalls(_anotherCsproj, _tempFolder, "Another"),
            .. ReversePackageDistributionTestProjects.GetReplaceCalls(_appCsproj, _tempFolder, "App")
        ], _editor.Calls);
        string output = _consoleOutput.ToString();
        Assert.Contains("1 remote-only projects are skipped", output, StringComparison.Ordinal);
        Assert.Contains("UseLocalPackages is off for project NoLocal, skipped", output, StringComparison.Ordinal);
        Assert.DoesNotContain($"Processing {_otherCsproj}", output, StringComparison.Ordinal);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 2");
    }

    [Fact]
    public async Task RunBody_WhenGroupIsAnother_ReplacesOnlyItsProjects()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("G2"));

        // Assert
        Assert.True(result);
        Assert.Equal(ReversePackageDistributionTestProjects.GetReplaceCalls(_otherCsproj, _tempFolder, "Other"),
            _editor.Calls);
        Assert.DoesNotContain("remote-only", _consoleOutput.ToString(), StringComparison.Ordinal);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 1");
    }

    //a project without a group belongs to the "__No Group__" group
    [Fact]
    public async Task RunBody_WhenGroupIsTheNoGroupName_ReplacesTheProjectsWithoutAGroup()
    {
        // Arrange
        string noGroupCsproj = AddProject("NoGroup", null);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut(SupportToolsParameters.FixProjectGroupName(null)));

        // Assert
        Assert.True(result);
        Assert.Equal(ReversePackageDistributionTestProjects.GetReplaceCalls(noGroupCsproj, _tempFolder, "NoGroup"),
            _editor.Calls);
    }

    [Fact]
    public async Task RunBody_WhenGroupHasNoProjects_ChangesNothing()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("G3"));

        // Assert
        Assert.True(result);
        Assert.Empty(_editor.Calls);
        VerifyLogged(LogLevel.Information, "Reverse package distribution finished. Projects processed: 0");
    }

    //a failed change of one project does not stop the next one, but the command fails
    [Fact]
    public async Task RunBody_WhenPackagesCannotBeRemoved_ReportsThemAndFails()
    {
        // Arrange
        _editor.FailingRemove = true;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal([$"remove {_anotherCsproj} Lib.A", $"remove {_appCsproj} Lib.A"], _editor.Calls);
        Assert.Contains($"Cannot remove package Lib.A from {_appCsproj}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateMenuCommand_WhenCalled_CreatesTheCommandOfTheCurrentGroup()
    {
        // Arrange
        var sut = new ReversePackageDistributionInGroupCliMenuCommandFactoryStrategy(
            new Mock<ILogger<ReversePackageDistributionInGroupCliMenuCommandFactoryStrategy>>().Object,
            new SupportToolsMenuParameters { ProjectGroupName = "G2" }, _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<ReversePackageDistributionInGroupCliMenuCommand>(command);
        Assert.EndsWith("of group G2", await CliMenuTestAccess.InvokeGetActionDescription(command),
            StringComparison.Ordinal);
    }

    //strategies are looked up by type name, so the group submenu shows the command only if its name is listed
    [Fact]
    public void ProjectGroupSubMenuCommandFactoryStrategyNames_ListTheStrategyRightAfterTheV3Sync()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectGroupSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(ReversePackageDistributionInGroupCliMenuCommandFactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy), strategyNames[index - 1]);
    }

    private ReversePackageDistributionInGroupCliMenuCommand CreateSut(string projectGroupName = "G1")
    {
        return new ReversePackageDistributionInGroupCliMenuCommand(_logger.Object, _parametersManager.Object,
            projectGroupName, () => _editor);
    }

    private string AddProject(string projectName, string? projectGroupName)
    {
        return ReversePackageDistributionTestProjects.AddProjectWithOwnPackage(_parameters, _tempFolder, projectName,
            projectGroupName);
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
