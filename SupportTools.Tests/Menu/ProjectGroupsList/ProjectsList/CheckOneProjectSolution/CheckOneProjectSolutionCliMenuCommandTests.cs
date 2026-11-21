using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectSolution;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectSolution;

//the local projects A and B have a missing solution file; the folder of Remote does not exist
[Collection(ConsoleCaptureCollection.Name)]
public sealed class CheckOneProjectSolutionCliMenuCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public CheckOneProjectSolutionCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        string missingSolution = Path.Combine(_tempFolder, "Missing.slnx");
        _parameters.Projects["A"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder, SolutionFileName = missingSolution
        };
        _parameters.Projects["B"] = new ProjectModel
        {
            ProjectFolderName = _tempFolder, SolutionFileName = missingSolution
        };
        _parameters.Projects["Remote"] = new ProjectModel
        {
            ProjectFolderName = Path.Combine(_tempFolder, "Missing"), SolutionFileName = missingSolution
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
        CheckOneProjectSolutionCliMenuCommand sut = CreateSut("A");

        // Assert
        Assert.Equal("Check Solution", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.False(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenProjectIsLocal_ChecksOnlyThisProject()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("A"));

        // Assert
        Assert.True(result);
        Assert.Equal(EProjectSolutionCheckStatus.NeedsAttention,
            _menuParameters.ProjectSolutionCheckResults["A"].Status);
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("B"));
    }

    //the files of a remote project are not on this computer
    [Fact]
    public async Task RunBody_WhenProjectIsRemote_SkipsIt()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("Remote"));

        // Assert
        Assert.True(result);
        Assert.False(_menuParameters.ProjectSolutionCheckResults.ContainsKey("Remote"));
        Assert.Contains("1 remote-only projects are skipped", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenProjectDoesNotExist_Fails()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("Missing"));

        // Assert
        Assert.False(result);
        Assert.Contains("Project Missing does not found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private CheckOneProjectSolutionCliMenuCommand CreateSut(string projectName)
    {
        return new CheckOneProjectSolutionCliMenuCommand(new Mock<ILogger>().Object, _parametersManager.Object,
            _menuParameters, projectName);
    }
}
