using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SyncAllProjectsAllGits;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.SyncAllProjectsAllGits;

//the gits point to missing local repositories, so every sync stops right after it prints "---=== <git> Collect"
//(no network, nothing is cloned). Local1 (git RepoA), Remote1 (folder missing, git RepoB)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncAllProjectsAllGitsCliMenuCommandV2Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public SyncAllProjectsAllGitsCliMenuCommandV2Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        AddProject("Local1", true, "RepoA");
        AddProject("Remote1", false, "RepoB");
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public void Constructor_WhenCreated_AsksBeforeRunningAndReloadsTheMenu()
    {
        // Act
        var sut = new SyncAllProjectsAllGitsCliMenuCommandV2(new Mock<ILogger>().Object, _parametersManager.Object);

        // Assert
        Assert.Equal("Sync All Projects All Gits V2", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreNotIncluded_SyncsOnlyTheLocalGits()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([("Include 1 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V2 Started", output, StringComparison.Ordinal);
        Assert.Contains("---=== RepoA Collect", output, StringComparison.Ordinal);
        Assert.DoesNotContain("---=== RepoB Collect", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreIncluded_SyncsTheirGitsToo()
    {
        // Arrange
        _answer = true;

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains("---=== RepoB Collect", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenEveryProjectIsLocal_DoesNotAsk()
    {
        // Arrange
        _parameters.Projects.Remove("Remote1");

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Empty(_questions);
        Assert.Contains("---=== RepoA Collect", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    private SyncAllProjectsAllGitsCliMenuCommandV2 CreateSut()
    {
        return new SyncAllProjectsAllGitsCliMenuCommandV2(new Mock<ILogger>().Object, _parametersManager.Object,
            (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            });
    }

    private void AddProject(string projectName, bool isLocal, string gitName)
    {
        string projectFolder = Path.Combine(_tempFolder, "Projects", projectName);
        if (isLocal)
        {
            Directory.CreateDirectory(projectFolder);
        }

        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectFolderName = projectFolder, GitProjectNames = [gitName]
        };
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = Path.Combine(_tempFolder, "Missing", $"{gitName}.git"),
            GitProjectFolderName = gitName,
            GitIgnorePatternName = "CSharp"
        };
    }
}
