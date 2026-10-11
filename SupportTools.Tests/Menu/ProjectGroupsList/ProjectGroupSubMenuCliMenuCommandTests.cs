using System;
using System.IO;
using System.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList;

//G1: LocalA, RemoteA and LocalB; G2: RemoteC only; G3: LocalD only
public sealed class ProjectGroupSubMenuCliMenuCommandTests : IDisposable
{
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly SupportToolsParameters _parameters = new();
    private readonly ParametersManager _parametersManager;
    private readonly ServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ProjectGroupSubMenuCliMenuCommandTests()
    {
        _parametersManager = new ParametersManager(null, _parameters);
        AddProject("LocalA", "G1", true);
        AddProject("RemoteA", "G1", false);
        AddProject("LocalB", "G1", true);
        AddProject("RemoteC", "G2", false);
        AddProject("LocalD", "G3", true);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_NamesMenuItemAfterGroupAndLoadsSubMenuOnRun()
    {
        // Act
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut("G1");

        // Assert
        Assert.Equal("G1", sut.Name);
        Assert.Equal(EMenuAction.LoadSubMenu, sut.MenuActionOnBodySuccess);
    }

    //the strategies of the group menu read the current group from SupportToolsMenuParameters
    [Fact]
    public void GetSubMenu_WhenCalled_MakesGroupCurrentAndCaptionsMenuWithIt()
    {
        // Arrange
        _menuParameters.ProjectGroupName = "G3";

        // Act
        CliMenuSet? result = CreateSut("G1").GetSubMenu();

        // Assert
        Assert.Equal("G1", _menuParameters.ProjectGroupName);
        Assert.Equal("G1", result?.Caption);
    }

    [Theory]
    [InlineData("G1", "local: 2, remote: 1")]
    [InlineData("G2", "remote: 1")]
    [InlineData("G3", "local: 1")]
    public void CountStatus_WhenNoProjectWasChecked_ShowsLocalAndRemoteCountsWithoutZeros(string groupName,
        string expected)
    {
        // Arrange
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut(groupName);

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal(expected, sut.StatusString);
        Assert.Equal(expected.Split(", "), sut.StatusColorParts?.Select(x => x.Text));
    }

    [Fact]
    public void CountStatus_WhenGroupHasRemoteProjects_ColorsRemoteCountDarkGray()
    {
        // Arrange
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut("G1");

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal(ConsoleColor.DarkGray, sut.StatusColorParts!.Single(x => x.Text == "remote: 1").Color);
    }

    //the statuses are counted in the order of EProjectBuildCheckStatus, a not checked project first
    [Fact]
    public void CountStatus_WhenProjectsWereChecked_ShowsCountsThenStatusBreakdownWithColors()
    {
        // Arrange
        _menuParameters.ProjectBuildCheckResults["LocalA"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.Success);
        _menuParameters.ProjectBuildCheckResults["LocalB"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.Success);
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut("G1");

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("local: 2, remote: 1, NotChecked: 1, Success: 2", sut.StatusString);
        Assert.Equal([ConsoleColor.Blue, ConsoleColor.Green],
            sut.StatusColorParts!.Skip(2).Select(x => x.Color));
    }

    [Fact]
    public void CountStatus_WhenCheckedProjectsHaveErrorsAndWarnings_AddsTheirSums()
    {
        // Arrange
        _menuParameters.ProjectBuildCheckResults["LocalA"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 2, 1);
        _menuParameters.ProjectBuildCheckResults["LocalB"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.SuccessWithWarnings, 0, 3);
        _menuParameters.ProjectBuildCheckResults["RemoteA"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 5, 0);
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut("G1");

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("local: 2, remote: 1, BuildFailed: 2, SuccessWithWarnings: 1, errors: 7, warnings: 4",
            sut.StatusString);
    }

    //results are kept per project: the result of a project of another group does not count here
    [Fact]
    public void CountStatus_WhenOnlyAProjectOfAnotherGroupWasChecked_ShowsOnlyTheCounts()
    {
        // Arrange
        _menuParameters.ProjectBuildCheckResults["LocalD"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 1, 0);
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut("G1");

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("local: 2, remote: 1", sut.StatusString);
    }

    [Fact]
    public void CountStatus_WhenRemoteOnlyProjectsAreHidden_CountsOnlyLocalProjects()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;
        _menuParameters.ProjectBuildCheckResults["RemoteA"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.BuildFailed, 5, 0);
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut("G1");

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("local: 2", sut.StatusString);
    }

    //a project without a group belongs to the "__No Group__" group
    [Fact]
    public void CountStatus_WhenGroupIsTheNoGroupName_CountsTheProjectsWithoutAGroup()
    {
        // Arrange
        AddProject("NoGroup", null, true);
        ProjectGroupSubMenuCliMenuCommand sut = CreateSut(SupportToolsParameters.FixProjectGroupName(null));

        // Act
        sut.CountStatus();

        // Assert
        Assert.Equal("local: 1", sut.StatusString);
    }

    private ProjectGroupSubMenuCliMenuCommand CreateSut(string groupName)
    {
        return new ProjectGroupSubMenuCliMenuCommand(_serviceProvider, _parametersManager, groupName,
            _menuParameters);
    }

    private void AddProject(string projectName, string? groupName, bool isLocal)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing")
        };
    }
}
