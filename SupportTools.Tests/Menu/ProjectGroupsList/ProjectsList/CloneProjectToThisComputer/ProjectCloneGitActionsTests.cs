using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

//runs the real git over a throwaway bare remote RepoA (a local path, no network) that holds the projects Lib and Core;
//Lib references Core. Project App (folder {temp}\Projects\App) has the git RepoA
[Collection(ConsoleCaptureCollection.Name)]
public sealed class ProjectCloneGitActionsTests : IDisposable
{
    private const string NoAddressMessage = "GitProjectAddress is empty for Git Repo with key NoAddress)";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ProjectCloneGitActionsTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        string remote = GitTestRepositories.CreateRemote(_tempFolder, "RepoA", new Dictionary<string, string>
        {
            [".gitignore"] = "bin/",
            [@"Lib\Lib.csproj"] = """
                                  <Project Sdk="Microsoft.NET.Sdk">
                                    <ItemGroup>
                                      <ProjectReference Include="..\Core\Core.csproj" />
                                    </ItemGroup>
                                  </Project>
                                  """,
            [@"Core\Core.csproj"] = """<Project Sdk="Microsoft.NET.Sdk" />"""
        });
        _parameters.WorkFolder = Path.Combine(_tempFolder, "Work");
        _parameters.Gits["RepoA"] = new GitDataModel
        {
            GitProjectAddress = remote, GitProjectFolderName = "RepoA", GitIgnorePatternName = "CSharp"
        };
        _projectFolder = Path.Combine(_tempFolder, "Projects", "App");
        Directory.CreateDirectory(_projectFolder);
        _parameters.Projects["App"] = new ProjectModel { ProjectFolderName = _projectFolder, GitProjectNames = ["RepoA"] };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    //the scan of Update Git Projects, for this git only: its clone in WorkFolder\Gits gives the records
    [Fact]
    public void ScanGitProjects_WhenGitCanBeCloned_AddsItsProjectsToGitProjects()
    {
        // Act
        bool result = CreateSut().ScanGitProjects("RepoA");

        // Assert
        Assert.True(result);
        GitProjectDataModel lib = _parameters.GitProjects["Lib"];
        Assert.Equal("RepoA", lib.GitName);
        Assert.Equal(@"RepoA\Lib", lib.ProjectRelativePath);
        Assert.Equal("Lib.csproj", lib.ProjectFileName);
        Assert.Equal(["Core"], lib.DependsOnProjectNames);
        Assert.Equal(@"RepoA\Core", _parameters.GitProjects["Core"].ProjectRelativePath);
    }

    //the scan reads the gits with the console, so a git without an address is reported
    [Fact]
    public void ScanGitProjects_WhenAGitHasNoAddress_ReportsItOnTheConsole()
    {
        // Arrange
        AddGitWithoutAddress();

        // Act
        CreateSut().ScanGitProjects("RepoA");

        // Assert
        Assert.Equal(1, CountOccurrences(_consoleOutput.ToString(), NoAddressMessage));
    }

    //the sync action of the project and the synchroniser both read the gits with the console
    [Fact]
    public void SyncGit_WhenAGitHasNoAddress_ReportsItOnTheConsoleForTheActionAndTheSynchroniser()
    {
        // Arrange
        AddGitWithoutAddress();

        // Act
        CreateSut().SyncGit("App", "RepoA");

        // Assert
        Assert.Equal(2, CountOccurrences(_consoleOutput.ToString(), NoAddressMessage));
    }

    [Fact]
    public void ScanGitProjects_WhenWorkFolderIsNotSet_ReturnsFalse()
    {
        // Arrange
        _parameters.WorkFolder = null;

        // Act
        bool result = CreateSut().ScanGitProjects("RepoA");

        // Assert
        Assert.False(result);
        Assert.Empty(_parameters.GitProjects);
    }

    [Fact]
    public void ScanGitProjects_WhenGitCannotBeCloned_ReturnsFalse()
    {
        // Arrange
        _parameters.Gits["RepoA"].GitProjectAddress = Path.Combine(_tempFolder, "Missing.git");

        // Act
        bool result = CreateSut().ScanGitProjects("RepoA");

        // Assert
        Assert.False(result);
        Assert.Empty(_parameters.GitProjects);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScanGitProjects_WhenGitWorkIsLoggedOrNot_LogsTheErrorsOnlyWhenLogged(bool logGitWork)
    {
        // Arrange
        _parameters.LogGitWork = logGitWork;
        _parameters.WorkFolder = null;

        // Act
        CreateSut().ScanGitProjects("RepoA");

        // Assert
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    value.ToString() == "supportToolsParameters.WorkFolder does not specified"), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), logGitWork ? Times.Once() : Times.Never());
    }

    [Fact]
    public void SyncGit_WhenGitIsAGitOfTheProject_ClonesItIntoTheProjectFolder()
    {
        // Act
        CreateSut().SyncGit("App", "RepoA");

        // Assert
        Assert.True(File.Exists(Path.Combine(_projectFolder, "RepoA", "Lib", "Lib.csproj")));
    }

    //a git that the project does not have gives no sync, so not even the work folder is touched
    [Fact]
    public void SyncGit_WhenGitIsNotInTheProject_DoesNothing()
    {
        // Arrange
        _parameters.Projects["App"].GitProjectNames.Clear();

        // Act
        CreateSut().SyncGit("App", "RepoA");

        // Assert
        Assert.False(Directory.Exists(Path.Combine(_projectFolder, "RepoA")));
        Assert.False(Directory.Exists(_parameters.WorkFolder));
    }

    private ProjectCloneGitActions CreateSut()
    {
        return new ProjectCloneGitActions(_logger.Object, _parametersManager.Object);
    }

    private void AddGitWithoutAddress()
    {
        _parameters.Gits["NoAddress"] =
            new GitDataModel { GitProjectFolderName = "NoAddress", GitIgnorePatternName = "CSharp" };
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }
}
