using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.CheckAllProjectsBuild;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.CheckAllProjectsBuild;

//no dotnet build runs: the local projects have no solution file, a missing one or a file that is not a solution
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckAllProjectsBuildCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private int _noIncrementalQuestions;

    public CheckAllProjectsBuildCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        string notASolution = Path.Combine(_tempFolder, "Readme.txt");
        File.WriteAllText(notASolution, "text");
        _parameters.Projects["NoSolution"] = new ProjectModel { ProjectFolderName = _tempFolder };
        _parameters.Projects["MissingSolution"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder, SolutionFileName = Path.Combine(_tempFolder, "Missing.slnx")
        };
        _parameters.Projects["NotASolution"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder, SolutionFileName = notASolution
        };
        _parameters.Projects["Remote"] = new ProjectModel
        {
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"), SolutionFileName = notASolution
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
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheMenu()
    {
        // Act
        var sut = new CheckAllProjectsBuildCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            _parametersManager.Object, _menuParameters);

        // Assert
        Assert.Equal("Check all projects build", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenCalled_ChecksEveryLocalProjectAndSkipsTheRemoteOnes()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(1, _noIncrementalQuestions);
        Assert.Equal(EProjectBuildCheckStatus.SolutionFileNameIsEmpty,
            _menuParameters.ProjectBuildCheckResults["NoSolution"].Status);
        Assert.Equal(EProjectBuildCheckStatus.SolutionFileDoesNotExists,
            _menuParameters.ProjectBuildCheckResults["MissingSolution"].Status);
        Assert.Equal(EProjectBuildCheckStatus.InvalidSolutionFile,
            _menuParameters.ProjectBuildCheckResults["NotASolution"].Status);
        Assert.False(_menuParameters.ProjectBuildCheckResults.ContainsKey("Remote"));
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //every project is checked again, so an old result of a project that is no longer checked disappears
    [Fact]
    public async Task RunBody_WhenOldResultsExist_ClearsThemFirst()
    {
        // Arrange
        _menuParameters.ProjectBuildCheckResults["Remote"] =
            new ProjectBuildCheckResult(EProjectBuildCheckStatus.Success);

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(_menuParameters.ProjectBuildCheckResults.ContainsKey("Remote"));
    }

    private CheckAllProjectsBuildCliMenuCommand CreateSut()
    {
        return new CheckAllProjectsBuildCliMenuCommand(new Mock<ILogger>().Object, "SupportTools",
            _parametersManager.Object, _menuParameters, () =>
            {
                _noIncrementalQuestions++;
                return false;
            });
    }
}
