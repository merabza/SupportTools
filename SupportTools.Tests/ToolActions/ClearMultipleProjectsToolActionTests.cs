using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using LibGitData.Models;
using LibTools.ToolActions;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolActions;

//runs the real clear over throwaway repositories. G1: Local1 (git RepoA, a repository with Lib\bin and Lib\obj)
//and Remote1 (folder missing; its git RepoB is a bare remote, so a clear that reached it would clone it);
//G2: Local2 (git RepoC with Lib\bin and Lib\obj)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ClearMultipleProjectsToolActionTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectsFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ClearMultipleProjectsToolActionTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _projectsFolder = Path.Combine(_tempFolder, "Projects");
        AddProject("Local1", "G1", "RepoA");
        CreateRepositoryWithBuildOutput(GitFolder("Local1", "RepoA"));
        AddProject("Remote1", "G1", "RepoB");
        _parameters.Gits["RepoB"].GitProjectAddress = GitTestRepositories.CreateRemote(_tempFolder, "RepoB",
            new Dictionary<string, string> { ["Readme.txt"] = "RepoB" });
        AddProject("Local2", "G2", "RepoC");
        CreateRepositoryWithBuildOutput(GitFolder("Local2", "RepoC"));
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    [Fact]
    public async Task Run_WhenAllProjects_ClearsTheLocalProjectsAndNeverClonesTheRemoteOnes()
    {
        // Act
        bool result = await Create(null, null).Run();

        // Assert
        Assert.True(result);
        AssertCleared(GitFolder("Local1", "RepoA"));
        AssertCleared(GitFolder("Local2", "RepoC"));
        Assert.False(Directory.Exists(Path.Combine(_projectsFolder, "Remote1")));
        string output = _consoleOutput.ToString();
        Assert.Contains("Clear Multiple Projects Started", output, StringComparison.Ordinal);
        //the projects go in name order
        Assert.True(output.IndexOf($"Process Folder {GitFolder("Local1", "RepoA")}", StringComparison.Ordinal) <
                    output.IndexOf($"Process Folder {GitFolder("Local2", "RepoC")}", StringComparison.Ordinal));
    }

    //with a seeders work folder, a project without a seeder project is skipped silently
    [Fact]
    public async Task Run_WhenProjectHasNoScaffoldSeederProject_SkipsItsSeederGitsWithoutMessages()
    {
        // Arrange
        _parameters.ScaffoldSeedersWorkFolder = Path.Combine(_tempFolder, "Seeders");

        // Act
        await Create(null, "Local1").Run();

        // Assert
        AssertCleared(GitFolder("Local1", "RepoA"));
        string output = _consoleOutput.ToString();
        Assert.DoesNotContain("ScaffoldSeederProjectName", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Gits folder is not found", output, StringComparison.Ordinal);
    }

    //SupportTools started from {folder}\bin\Debug\{framework} does not clear the folder of its own sources
    [Fact]
    public async Task Run_WhenStartedFromABinDebugFolder_KeepsTheProgramSourcesUncleared()
    {
        // Arrange
        string programRepository = AddProgramProject();

        // Act
        await CreateFrom(Path.Combine(programRepository, "bin", "Debug", "net10.0")).Run();

        // Assert
        AssertNotCleared(programRepository);
        AssertCleared(GitFolder("Local1", "RepoA"));
    }

    [Theory]
    [InlineData("bin", "Release")]
    [InlineData("out", "Debug")]
    public async Task Run_WhenNotStartedFromABinDebugFolder_ClearsTheProgramSourcesToo(string outputFolder,
        string configurationFolder)
    {
        // Arrange
        string programRepository = AddProgramProject();

        // Act
        await CreateFrom(Path.Combine(programRepository, outputFolder, configurationFolder, "net10.0")).Run();

        // Assert
        AssertCleared(programRepository);
    }

    [Fact]
    public async Task Run_WhenGroupIsGiven_ClearsOnlyItsProjects()
    {
        // Act
        await Create("G2", null).Run();

        // Assert
        AssertCleared(GitFolder("Local2", "RepoC"));
        AssertNotCleared(GitFolder("Local1", "RepoA"));
    }

    [Fact]
    public async Task Run_WhenProjectIsGiven_ClearsOnlyIt()
    {
        // Act
        await Create(null, "Local1").Run();

        // Assert
        AssertCleared(GitFolder("Local1", "RepoA"));
        AssertNotCleared(GitFolder("Local2", "RepoC"));
    }

    [Fact]
    public async Task Run_WhenSeedersWorkFolderIsSet_ClearsTheScaffoldSeederGitsToo()
    {
        // Arrange
        string seederRepository = AddScaffoldSeederGit();

        // Act
        await Create(null, "Local1").Run();

        // Assert
        AssertCleared(seederRepository);
    }

    [Fact]
    public async Task Run_WhenSeedersWorkFolderIsNotSet_WarnsAndSkipsTheScaffoldSeederGits()
    {
        // Arrange
        AddScaffoldSeederGit();
        _parameters.ScaffoldSeedersWorkFolder = null;

        // Act
        await Create(null, "Local1").Run();

        // Assert
        string output = _consoleOutput.ToString();
        Assert.Contains("ScaffoldSeedersWorkFolder is not specified", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Gits folder is not found", output, StringComparison.Ordinal);
    }

    private ClearMultipleProjectsToolAction Create(string? projectGroupName, string? projectName)
    {
        return ClearMultipleProjectsToolAction.Create(new Mock<ILogger>().Object, _parametersManager.Object,
            projectGroupName, projectName, true);
    }

    private ClearMultipleProjectsToolAction CreateFrom(string baseFolder)
    {
        return ClearMultipleProjectsToolAction.Create(new Mock<ILogger>().Object, _parametersManager.Object, null,
            null, true, baseFolder);
    }

    //the sources of the running program: project Tool with the git ToolSources ({temp}\Projects\Tool\ToolSources)
    private string AddProgramProject()
    {
        AddProject("Tool", "G3", "ToolSources");
        string programRepository = GitFolder("Tool", "ToolSources");
        CreateRepositoryWithBuildOutput(programRepository);
        return programRepository;
    }

    private string AddScaffoldSeederGit()
    {
        _parameters.ScaffoldSeedersWorkFolder = Path.Combine(_tempFolder, "Seeders");
        _parameters.Gits["RepoS"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/RepoS.git", GitProjectFolderName = "RepoS", GitIgnorePatternName = "CSharp"
        };
        ProjectModel project = _parameters.Projects["Local1"];
        project.ScaffoldSeederProjectName = "Local1";
        project.ScaffoldSeederGitProjectNames.Add("RepoS");
        string seederRepository =
            Path.Combine(_parameters.ScaffoldSeedersWorkFolder, "Local1", "Local1ScaffoldSeeder", "RepoS");
        CreateRepositoryWithBuildOutput(seederRepository);
        return seederRepository;
    }

    private void AddProject(string projectName, string groupName, string gitName)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName,
            ProjectFolderName = Path.Combine(_projectsFolder, projectName),
            GitProjectNames = [gitName]
        };
        _parameters.Gits[gitName] = new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{gitName}.git",
            GitProjectFolderName = gitName,
            GitIgnorePatternName = "CSharp"
        };
    }

    //a project folder with a source file is kept; only its bin and obj go
    private static void CreateRepositoryWithBuildOutput(string repository)
    {
        GitTestRepositories.InitRepository(repository);
        string libFolder = Path.Combine(repository, "Lib");
        Directory.CreateDirectory(Path.Combine(libFolder, "bin", "Debug"));
        Directory.CreateDirectory(Path.Combine(libFolder, "obj"));
        File.WriteAllText(Path.Combine(libFolder, "Lib.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        File.WriteAllText(Path.Combine(libFolder, "Class1.cs"), "namespace Lib;");
        File.WriteAllText(Path.Combine(libFolder, "bin", "Debug", "Lib.dll"), "dll");
        File.WriteAllText(Path.Combine(libFolder, "obj", "project.assets.json"), "{}");
    }

    private string GitFolder(string projectName, string gitName)
    {
        return Path.Combine(_projectsFolder, projectName, gitName);
    }

    private static void AssertCleared(string repository)
    {
        Assert.False(Directory.Exists(Path.Combine(repository, "Lib", "bin")));
        Assert.False(Directory.Exists(Path.Combine(repository, "Lib", "obj")));
        Assert.True(File.Exists(Path.Combine(repository, "Lib", "Class1.cs")));
    }

    private static void AssertNotCleared(string repository)
    {
        Assert.True(Directory.Exists(Path.Combine(repository, "Lib", "bin")));
        Assert.True(Directory.Exists(Path.Combine(repository, "Lib", "obj")));
    }
}
