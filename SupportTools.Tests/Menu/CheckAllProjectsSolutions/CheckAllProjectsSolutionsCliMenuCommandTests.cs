using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.CheckAllProjectsSolutions;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.CheckAllProjectsSolutions;

//the local projects have no solution file or a missing one; the folder of Remote does not exist
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckAllProjectsSolutionsCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public CheckAllProjectsSolutionsCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.Projects["NoSolution"] = new ProjectModel { ProjectFolderName = _tempFolder };
        _parameters.Projects["MissingSolution"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder, SolutionFileName = Path.Combine(_tempFolder, "Missing.slnx")
        };
        _parameters.Projects["Remote"] = new ProjectModel
        {
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"),
            SolutionFileName = Path.Combine(_tempFolder, "Missing.slnx")
        };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    //the check only reads files and is fast, so it asks nothing before running
    [Fact]
    public void Constructor_WhenCreated_RunsWithoutAskingAndReloadsTheMenu()
    {
        // Act
        CheckAllProjectsSolutionsCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal("Check Solutions", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.False(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenCalled_ChecksEveryLocalProjectAndSkipsTheRemoteOnes()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention,
            _menuParameters.ProjectSolutionCheckResults["MissingSolution"].Status);
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("NoSolution"));
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("Remote"));
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //every project is checked again, so an old result of a project that is no longer checked disappears
    [Fact]
    public async Task RunBody_WhenOldResultsExist_ClearsThemFirst()
    {
        // Arrange
        _menuParameters.ProjectSolutionCheckResults["Remote"] =
            new ProjectSolutionCheckResult(EProjectSolutionCheckStatus.Ok, []);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("Remote"));
    }

    private CheckAllProjectsSolutionsCliMenuCommand CreateSut()
    {
        return new CheckAllProjectsSolutionsCliMenuCommand(new Mock<ILogger>().Object, _parametersManager.Object,
            _menuParameters);
    }
}
