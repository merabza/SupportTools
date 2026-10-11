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
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

//the gits point to missing local repositories, so every sync stops right after it prints "---=== <git> Collect"
//(no network, nothing is cloned). G1: Local1 (RepoA), Remote1 (RepoB); G2: Local2 (RepoC), Remote2 (RepoD)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncOneGroupAllProjectsGitsCliMenuCommandV2Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly List<(string Question, bool DefaultValue)> _questions = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private bool _answer;

    public SyncOneGroupAllProjectsGitsCliMenuCommandV2Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
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
        var sut = new SyncOneGroupAllProjectsGitsCliMenuCommandV2(new Mock<ILogger>().Object,
            _parametersManager.Object, "G1");

        // Assert
        Assert.Equal("Sync One Group All Projects Gits V2", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    //the question counts the remote projects of this group only
    [Fact]
    public async Task RunBody_WhenRemoteProjectsAreNotIncluded_SyncsOnlyTheLocalGitsOfTheGroup()
    {
        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut("G2"));

        // Assert
        Assert.True(result);
        Assert.Equal([("Include 2 remote-only projects (their gits will be cloned to this computer)?", false)],
            _questions);
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V2 Started", output, StringComparison.Ordinal);
        Assert.Contains("---=== RepoC Collect", output, StringComparison.Ordinal);
        Assert.DoesNotContain("---=== RepoD Collect", output, StringComparison.Ordinal);
        Assert.DoesNotContain("---=== RepoA Collect", output, StringComparison.Ordinal);
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
        Assert.Contains("---=== RepoA Collect", output, StringComparison.Ordinal);
        Assert.Contains("---=== RepoB Collect", output, StringComparison.Ordinal);
        Assert.DoesNotContain("---=== RepoD Collect", output, StringComparison.Ordinal);
    }

    private SyncOneGroupAllProjectsGitsCliMenuCommandV2 CreateSut(string projectGroupName)
    {
        return new SyncOneGroupAllProjectsGitsCliMenuCommandV2(new Mock<ILogger>().Object,
            _parametersManager.Object, projectGroupName, (question, defaultValue) =>
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
