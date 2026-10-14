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
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

//the gits point to missing local repositories, so the clone of every folder fails at once (no network, nothing is
//cloned) and the output names the synced gits. Remote1 (folder missing) has the main git RepoA and the ScaffoldSeeder
//git RepoS; Local1 has RepoB
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3Tests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3Tests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        _parameters.ScaffoldSeedersWorkFolder = Path.Combine(_tempFolder, "Seeders");
        AddGit("RepoA");
        AddGit("RepoB");
        AddGit("RepoS");
        _parameters.Projects["Remote1"] = new ProjectModel
        {
            ProjectFolderName = Path.Combine(_tempFolder, "Projects", "Remote1"),
            GitProjectNames = ["RepoA"],
            ScaffoldSeederProjectName = "Remote1",
            ScaffoldSeederGitProjectNames = ["RepoS"]
        };
        _parameters.Projects["Local1"] = new ProjectModel
        {
            ProjectFolderName = Directory.CreateDirectory(Path.Combine(_tempFolder, "Projects", "Local1")).FullName,
            GitProjectNames = ["RepoB"]
        };
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
        var sut = CreateSut("Remote1");

        // Assert
        Assert.Equal("Sync One Project All Gits With Scaffold Seeders V3", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.True(CliMenuTestAccess.GetAskRunAction(sut));
    }

    //like V2, the command of one project syncs its main and ScaffoldSeeder gits, also when the project is remote
    [Fact]
    public async Task RunBody_WhenTheProjectIsRemote_SyncsItsGitsWithoutAsking()
    {
        // Act
        await CliMenuTestAccess.InvokeRunBody(CreateSut("Remote1"));

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("Sync Multiple Projects Gits V3 Started", output, StringComparison.Ordinal);
        Assert.Contains("Synced 2 gits, 2 folders", output, StringComparison.Ordinal);
        Assert.Contains("RepoA  ", output, StringComparison.Ordinal);
        Assert.Contains("RepoS  ", output, StringComparison.Ordinal);
        Assert.DoesNotContain("RepoB  ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMenuCommand_WhenCalled_CreatesTheV3CommandOfTheProject()
    {
        // Arrange
        var sut = new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3FactoryStrategy(
            new Mock<ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3FactoryStrategy>>().Object,
            new Mock<IHttpClientFactory>().Object, new SupportToolsMenuParameters { ProjectName = "Remote1" },
            _parametersManager.Object);

        // Act
        CliMenuCommand command = sut.CreateMenuCommand();

        // Assert
        Assert.IsType<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3>(command);
    }

    //strategies are looked up by type name, so the project submenu shows the command only if its name is listed
    [Fact]
    public void ProjectSubMenuCommandFactoryStrategyNames_ListTheV3StrategyRightAfterTheV2One()
    {
        // Act
        List<string> strategyNames = MenuData.ProjectSubMenuCommandFactoryStrategyNames;

        // Assert
        int index = strategyNames.IndexOf(
            nameof(SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3FactoryStrategy));
        Assert.True(index > 0);
        Assert.Equal(nameof(SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy),
            strategyNames[index - 1]);
    }

    private SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3 CreateSut(string projectName)
    {
        return new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, _parametersManager.Object, projectName);
    }

    private void AddGit(string gitName)
    {
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = Path.Combine(_tempFolder, "Missing", $"{gitName}.git"),
            GitProjectFolderName = gitName,
            GitIgnorePatternName = "CSharp"
        };
    }
}
