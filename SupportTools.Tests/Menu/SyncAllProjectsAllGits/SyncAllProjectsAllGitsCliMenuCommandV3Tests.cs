using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.SyncAllProjectsAllGits;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.SyncAllProjectsAllGits;

//the gits point to missing local repositories, so the clone of every folder fails at once (no network, nothing is
//cloned) and the summary counts the synced gits. Local1 (git RepoA), Remote1 (folder missing, git RepoB). There is no
//SupportToolsServer, so every git is checked with git itself
//Every git also has its work folder clone, so the summary counts two folders per git
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncAllProjectsAllGitsCliMenuCommandV3Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public SyncAllProjectsAllGitsCliMenuCommandV3Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        _parameters.ScaffoldSeedersWorkFolder = Path.Combine(_tempFolder, "Seeders");
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
        var sut = new SyncAllProjectsAllGitsCliMenuCommandV3(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object);

        // Assert
        Assert.Equal("Sync All Projects All Gits V3", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreNotIncluded_SyncsOnlyTheLocalGits()
    {
        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal([("Include 1 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V3 Started", output, StringComparison.Ordinal);
        Assert.Contains("Synced 1 gits, 2 folders", output, StringComparison.Ordinal);
        Assert.Contains("RepoA  ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RepoB  ", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreIncluded_SyncsTheirGitsToo()
    {
        // Arrange
        _answer = true;

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Synced 2 gits, 4 folders", output, StringComparison.Ordinal);
        Assert.Contains("RepoB  ", output, StringComparison.Ordinal);
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
        Assert.Contains("Synced 1 gits, 2 folders", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheV3Command()
    {
        // Arrange
        var sut = new SyncAllProjectsAllGitsCliMenuCommandV3FactoryStrategy(
            new Mock<ILogger<SyncAllProjectsAllGitsCliMenuCommandV3FactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<SyncAllProjectsAllGitsCliMenuCommandV3>(command);
    }

    //strategies are looked up by type name, so the main menu shows the command only if its name is listed
    [Fact]
    public void MainMenuCommandFactoryStrategyNames_ListTheV3StrategyRightAfterTheV2One()
    {
        // Act
        List<string> strategyNames = MenuData.MainMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(SyncAllProjectsAllGitsCliMenuCommandV3FactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy), strategyNames[index - 1]);
    }

    private SyncAllProjectsAllGitsCliMenuCommandV3 CreateSut()
    {
        return new SyncAllProjectsAllGitsCliMenuCommandV3(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, (question, defaultValue) =>
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
