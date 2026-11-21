using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsSolutions;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.CheckGroupProjectsSolutions;

//the local projects have a missing solution file. G1: Local1 and Remote1; G2: Local2
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckGroupProjectsSolutionsCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public CheckGroupProjectsSolutionsCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        string missingSolution = Path.Combine(_tempFolder, "Missing.slnx");
        _parameters.Projects["Local1"] = new ProjectModel
        {
            ProjectGroupName = "G1", ProjectFolderName = _tempFolder, SolutionFileName = missingSolution
        };
        _parameters.Projects["Remote1"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"),
            SolutionFileName = missingSolution
        };
        _parameters.Projects["Local2"] = new ProjectModel
        {
            ProjectGroupName = "G2", ProjectFolderName = _tempFolder, SolutionFileName = missingSolution
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_RunsWithoutAskingAndReloadsTheMenu()
    {
        // Act
        CheckGroupProjectsSolutionsCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Check Solutions", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.False(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenCalled_ChecksOnlyTheLocalProjectsOfTheGroup()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention,
            _menuParameters.ProjectSolutionCheckResults["Local1"].Status);
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("Remote1"));
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("Local2"));
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the results of the other groups stay
    [Fact]
    public async Task RunBody_WhenOtherGroupsHaveResults_KeepsThem()
    {
        // Arrange
        var otherResult = new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.Ok, []);
        _menuParameters.ProjectSolutionCheckResults["Local2"] = otherResult;

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Same(otherResult, _menuParameters.ProjectSolutionCheckResults["Local2"]);
    }

    private CheckGroupProjectsSolutionsCliMenuCommand CreateSut()
    {
        return new CheckGroupProjectsSolutionsCliMenuCommand(new Mock<ILogger>().Object, _parametersManager.Object,
            _menuParameters, "G1");
    }
}
