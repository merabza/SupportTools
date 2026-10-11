using System;
using System.Globalization;
using System.IO;
using LibGitData;
using LibGitData.Models;
using LibGitWork.Models;
using LibGitWork.ToolCommandParameters;
using SupportTools.Tests.GitProjects;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolCommandParameters;

//project Front (folder D:\Projects\Front): gits FrontBack (plain folder) and FrontSpa
//({SpaProjectFolderRelativePath}\ClientApp, SpaProjectName FrontApp); ScaffoldSeeder git FrontSeeder
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitSyncParametersTests : IDisposable
{
    private const string ProjectName = "Front";
    private const string ProjectFolder = @"D:\Projects\Front";
    private const string BackGitName = "FrontBack";
    private const string SpaGitName = "FrontSpa";
    private const string SeederGitName = "FrontSeeder";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new() { GitExecutablePath = @"C:\Tools\git.exe" };
    private readonly ProjectModel _project;

    public GitSyncParametersTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _project = new ProjectModel
        {
            ProjectFolderName = ProjectFolder,
            SpaProjectName = "FrontApp",
            ScaffoldSeederProjectName = ProjectName,
            GitProjectNames = [BackGitName, SpaGitName],
            ScaffoldSeederGitProjectNames = [SeederGitName]
        };
        _parameters.Projects[ProjectName] = _project;
        _parameters.ScaffoldSeedersWorkFolder = @"D:\Seeders";
        _parameters.Gits[BackGitName] = CreateGit(BackGitName, BackGitName);
        _parameters.Gits[SpaGitName] =
            CreateGit(SpaGitName, $@"{GitDataModel.SpaProjectFolderRelativePathName}\ClientApp");
        _parameters.Gits[SeederGitName] = CreateGit(SeederGitName, SeederGitName);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_KeepsTheValuesAndAllowsSaving()
    {
        // Arrange
        var gitData = new GitData
        {
            GitProjectAddress = "a", GitProjectFolderName = "f", GitProjectName = "n", GitIgnorePatternName = "p"
        };

        // Act
        var sut = new GitSyncParameters(gitData, @"D:\Gits");

        // Assert
        Assert.Same(gitData, sut.GitData);
        Assert.Equal(@"D:\Gits", sut.GitsFolder);
        Assert.Null(sut.GitExecutablePath);
        Assert.True(sut.CheckBeforeSave());
    }

    [Fact]
    public void Create_WhenMainGitExists_ReturnsItsFolderAndTheGitPath()
    {
        // Act
        GitSyncParameters? result = Create(EGitCol.Main, BackGitName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProjectFolder, result.GitsFolder);
        Assert.Equal(@"C:\Tools\git.exe", result.GitExecutablePath);
        Assert.Equal("git@github.com:x/FrontBack.git", result.GitData.GitProjectAddress);
        Assert.Equal(BackGitName, result.GitData.GitProjectFolderName);
        Assert.Equal("CSharp", result.GitData.GitIgnorePatternName);
        Assert.Empty(_consoleOutput.ToString());
    }

    [Fact]
    public void Create_WhenScaffoldSeederGitExists_ReturnsTheSeederFolder()
    {
        // Act
        GitSyncParameters? result = Create(EGitCol.ScaffoldSeed, SeederGitName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.Combine(@"D:\Seeders", ProjectName, "FrontScaffoldSeeder"), result.GitsFolder);
        Assert.Equal(SeederGitName, result.GitData.GitProjectFolderName);
    }

    [Fact]
    public void Create_WhenProjectDoesNotExist_ReportsItAndReturnsNull()
    {
        // Act
        GitSyncParameters? result = GitSyncParameters.Create(null, _parameters, "Missing", EGitCol.Main,
            BackGitName, true);

        // Assert
        Assert.Null(result);
        AssertOutput("Git Project with name Missing does not exists");
    }

    [Fact]
    public void Create_WhenGitIsNotInTheProjectList_ReportsItAndReturnsNull()
    {
        // Act
        GitSyncParameters? result = Create(EGitCol.ScaffoldSeed, BackGitName);

        // Assert
        Assert.Null(result);
        AssertOutput("Git Project with name FrontBack does not exists");
    }

    [Fact]
    public void Create_WhenGitIsNotInGits_ReportsItAndReturnsNull()
    {
        // Arrange
        _parameters.Gits.Remove(BackGitName);

        // Act
        GitSyncParameters? result = Create(EGitCol.Main, BackGitName);

        // Assert
        Assert.Null(result);
        AssertOutput("Git Project with name FrontBack does not exists in project Front");
    }

    //GitRepos.Create leaves out a git without an address
    [Fact]
    public void Create_WhenGitHasNoAddress_ReportsItAndReturnsNull()
    {
        // Arrange
        _parameters.Gits[BackGitName].GitProjectAddress = " ";

        // Act
        GitSyncParameters? result = Create(EGitCol.Main, BackGitName);

        // Assert
        Assert.Null(result);
        AssertOutput("Git Project with name FrontBack does not exists in gits list");
    }

    [Fact]
    public void Create_WhenProjectFolderIsNotSpecified_ReportsItAndReturnsNull()
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel { GitProjectNames = [BackGitName] };

        // Act
        GitSyncParameters? result = Create(EGitCol.Main, BackGitName);

        // Assert
        Assert.Null(result);
        AssertOutput("Gits folder not found");
    }

    [Fact]
    public void Create_WhenSpaGitAndRecordIsMissing_ReturnsNullWithClearError()
    {
        // Act
        GitSyncParameters? result = Create(EGitCol.Main, SpaGitName);

        // Assert
        Assert.Null(result);
        AssertOutput(SpaProjectFolderHelperTests.MissingMessage);
        Assert.DoesNotContain("does not exists in gits list", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the other gits of the project still sync, so Clone Project To This Computer can clone them first
    [Fact]
    public void Create_WhenPlainGitAndRecordIsMissing_ReturnsItsParameters()
    {
        // Act
        GitSyncParameters? result = Create(EGitCol.Main, BackGitName);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(SpaProjectFolderHelperTests.MissingMessage, _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenSpaGitAndRecordExists_PlacesGitInSpaProjectFolder()
    {
        // Arrange
        _parameters.GitProjects["FrontApp"] = new GitProjectDataModel
        {
            GitName = BackGitName, ProjectRelativePath = @"FrontBack\FrontApp", ProjectFileName = "FrontApp.esproj"
        };

        // Act
        GitSyncParameters? result = Create(EGitCol.Main, SpaGitName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Path.Combine(@"FrontBack\FrontApp", "ClientApp"), result.GitData.GitProjectFolderName);
    }

    private GitSyncParameters? Create(EGitCol gitCol, string gitProjectName)
    {
        return GitSyncParameters.Create(null, _parameters, ProjectName, gitCol, gitProjectName, true);
    }

    //a whole error line, so that a message is not taken for a longer one that starts with it
    private void AssertOutput(string expected)
    {
        Assert.Contains($"[ERROR] {expected}{Environment.NewLine}", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    private static GitDataModel CreateGit(string gitName, string folderName)
    {
        return new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{gitName}.git",
            GitProjectFolderName = folderName,
            GitIgnorePatternName = "CSharp"
        };
    }
}
