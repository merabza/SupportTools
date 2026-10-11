using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsBuild;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.CheckGroupProjectsBuild;

//no dotnet build runs: the projects have no solution file. G1: Local1 and Remote1; G2: Local2
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckGroupProjectsBuildCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private int _noIncrementalQuestions;

    public CheckGroupProjectsBuildCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Projects["Local1"] = new ProjectModel { ProjectGroupName = "G1", ProjectFolderName = _tempFolder };
        _parameters.Projects["Remote1"] = new ProjectModel
        {
            ProjectGroupName = "G1", ProjectFolderName = Path.Combine(_tempFolder, "Missing")
        };
        _parameters.Projects["Local2"] = new ProjectModel { ProjectGroupName = "G2", ProjectFolderName = _tempFolder };
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
        var sut = new CheckGroupProjectsBuildCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            _parametersManager.Object, _menuParameters, "G1");

        // Assert
        Assert.Equal("Check group projects build", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenCalled_ChecksOnlyTheLocalProjectsOfTheGroup()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(1, _noIncrementalQuestions);
        Assert.Equal(EProjectBuildCheckStatus.SolutionFileNameIsEmpty,
            _menuParameters.ProjectBuildCheckResults["Local1"].Status);
        Assert.False(_menuParameters.ProjectBuildCheckResults.ContainsKey("Remote1"));
        Assert.False(_menuParameters.ProjectBuildCheckResults.ContainsKey("Local2"));
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the results of the other groups stay
    [Fact]
    public async Task RunBody_WhenOtherGroupsHaveResults_KeepsThem()
    {
        // Arrange
        var otherResult = new ProjectBuildCheckResult(EProjectBuildCheckStatus.Success);
        _menuParameters.ProjectBuildCheckResults["Local2"] = otherResult;

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Same(otherResult, _menuParameters.ProjectBuildCheckResults["Local2"]);
    }

    private CheckGroupProjectsBuildCliMenuCommand CreateSut()
    {
        return new CheckGroupProjectsBuildCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            _parametersManager.Object, _menuParameters, "G1", () =>
            {
                _noIncrementalQuestions++;
                return true;
            });
    }
}
