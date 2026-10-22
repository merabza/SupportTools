using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.OpenAllProjectsByVisualStudio;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.OpenAllProjectsByVisualStudio;

//Visual Studio never starts: finding devenv, opening a solution and the pause between the openings are fakes.
//G1: beta and Alpha (solutions exist), NoSolution, MissingSolution and Remote; G2: Other (solution exists)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class OpenAllProjectsByVisualStudioCliMenuCommandTests : IDisposable
{
    private const string DevEnvPath = @"C:\VS\devenv.exe";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly List<string> _events = [];
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private string? _devEnvPath = DevEnvPath;

    public OpenAllProjectsByVisualStudioCliMenuCommandTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        AddProject("beta", "G1", true, true);
        AddProject("Alpha", "G1", true, true);
        AddProject("NoSolution", "G1", true, null);
        AddProject("MissingSolution", "G1", true, false);
        AddProject("Remote", "G1", false, true);
        AddProject("Other", "G2", true, true);
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
        var sut = new OpenAllProjectsByVisualStudioCliMenuCommand(_parametersManager.Object, "G1", _logger.Object);

        // Assert
        Assert.Equal("Open all projects By Visual Studio", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    //the local projects of the group with an existing solution open in name order, with a pause between the openings
    [Fact]
    public async Task RunBody_WhenSolutionsExist_OpensThemInNameOrderWithPausesBetween()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([
            "detect", $"open {DevEnvPath} {Solution("Alpha")}", "delay", $"open {DevEnvPath} {Solution("beta")}"
        ], _events);
        string output = _consoleOutput.ToString();
        Assert.Contains($"Opening Alpha solution {Solution("Alpha")}...", output, StringComparison.Ordinal);
        Assert.Contains("Project NoSolution does not have a solution file", output, StringComparison.Ordinal);
        //a project without a solution file is reported once and not checked any further
        Assert.DoesNotContain("Project NoSolution solution file", output, StringComparison.Ordinal);
        Assert.Contains($"Project MissingSolution solution file {Solution("MissingSolution")} does not exists", output,
            StringComparison.Ordinal);
        Assert.Contains("1 remote-only projects are skipped", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenOnlyOneSolutionExists_OpensItWithoutPause()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("G2"));

        // Assert
        Assert.True(result);
        Assert.Equal(["detect", $"open {DevEnvPath} {Solution("Other")}"], _events);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task RunBody_WhenVisualStudioIsNotFound_ReportsItAndFails(string? devEnvPath)
    {
        // Arrange
        _devEnvPath = devEnvPath;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal(["detect"], _events);
        Assert.Contains("Could not find Visual Studio devenv.exe", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private OpenAllProjectsByVisualStudioCliMenuCommand CreateSut(string projectGroupName = "G1")
    {
        return new OpenAllProjectsByVisualStudioCliMenuCommand(_parametersManager.Object, projectGroupName,
            _logger.Object, logger =>
            {
                Assert.Same(_logger.Object, logger);
                _events.Add("detect");
                return _devEnvPath;
            }, (devEnvPath, solutionFileName, logger) =>
            {
                Assert.Same(_logger.Object, logger);
                _events.Add($"open {devEnvPath} {solutionFileName}");
            }, _ =>
            {
                _events.Add("delay");
                return Task.CompletedTask;
            });
    }

    //solutionExists: true - the solution file exists, false - it is named but missing, null - none is named
    private void AddProject(string projectName, string groupName, bool isLocal, bool? solutionExists)
    {
        string? solutionFileName = solutionExists is null ? null : Solution(projectName);
        if (solutionExists == true)
        {
            File.WriteAllText(solutionFileName!, "<Solution />");
        }

        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing"),
            SolutionFileName = solutionFileName
        };
    }

    private string Solution(string projectName)
    {
        return Path.Combine(_tempFolder, $"{projectName}.slnx");
    }
}
