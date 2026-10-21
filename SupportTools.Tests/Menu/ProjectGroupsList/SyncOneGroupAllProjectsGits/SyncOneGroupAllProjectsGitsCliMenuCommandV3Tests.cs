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
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

//the gits point to missing local repositories, so the clone of every folder fails at once (no network, nothing is
//cloned) and the output names the synced gits. G1: Local1 (RepoA), Remote1 (RepoB); G2: Local2 (RepoC), Remote2 (RepoD),
//Remote3 (RepoE)
//Every git also has its work folder clone, so the summary counts two folders per git
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncOneGroupAllProjectsGitsCliMenuCommandV3Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public SyncOneGroupAllProjectsGitsCliMenuCommandV3Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        _parameters.ScaffoldSeedersWorkFolder = Path.Combine(_tempFolder, "Seeders");
        AddProject("Local1", "G1", true, "RepoA");
        AddProject("Remote1", "G1", false, "RepoB");
        AddProject("Local2", "G2", true, "RepoC");
        AddProject("Remote2", "G2", false, "RepoD");
        AddProject("Remote3", "G2", false, "RepoE");
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
        var sut = new SyncOneGroupAllProjectsGitsCliMenuCommandV3(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, "G1");

        // Assert
        Assert.Equal("Sync One Group All Projects Gits V3", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    //the question counts the remote projects of this group only
    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreNotIncluded_SyncsOnlyTheLocalGitsOfTheGroup()
    {
        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut("G2"));

        // Assert
        Assert.Equal([("Include 2 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V3 Started", output, StringComparison.Ordinal);
        Assert.Contains("Synced 1 gits, 2 folders", output, StringComparison.Ordinal);
        Assert.Contains("RepoC  ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RepoD  ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RepoA  ", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreIncluded_SyncsTheRemoteGitsOfTheGroupOnly()
    {
        // Arrange
        _answer = true;

        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut("G1"));

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Synced 2 gits, 4 folders", output, StringComparison.Ordinal);
        Assert.Contains("RepoA  ", output, StringComparison.Ordinal);
        Assert.Contains("RepoB  ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RepoD  ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheV3CommandOfTheGroup()
    {
        // Arrange
        var sut = new SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy(
            new Mock<ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new SupportToolsMenuParameters { ProjectGroupName = "G1" },
            _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<SyncOneGroupAllProjectsGitsCliMenuCommandV3>(command);
    }

    //strategies are looked up by type name, so the group submenu shows the command only if its name is listed
    [Fact]
    public void ProjectGroupSubMenuCommandFactoryStrategyNames_ListTheV3StrategyRightAfterTheV2One()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectGroupSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(nameof(SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy), strategyNames[index - 1]);
    }

    private SyncOneGroupAllProjectsGitsCliMenuCommandV3 CreateSut(string projectGroupName)
    {
        return new SyncOneGroupAllProjectsGitsCliMenuCommandV3(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, projectGroupName,
            (question, defaultValue) =>
            {
                _questions.Add((question, defaultValue));
                return _answer;
            });
    }

    private void AddProject(string projectName, string groupName, bool isLocal, string gitName)
    {
        string projectFolder = Path.Combine(_tempFolder, "Projects", projectName);
        if (isLocal)
        {
            Directory.CreateDirectory(projectFolder);
        }

        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName, ProjectFolderName = projectFolder, GitProjectNames = [gitName]
        };
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = Path.Combine(_tempFolder, "Missing", $"{gitName}.git"),
            GitProjectFolderName = gitName,
            GitIgnorePatternName = "CSharp"
        };
    }
}
