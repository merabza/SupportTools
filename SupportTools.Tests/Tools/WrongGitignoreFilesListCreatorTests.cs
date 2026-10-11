using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LibGitData.Models;
using LibGitWork;
using LibGitWork.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Tools;

//templates CSharp ("bin/") and Node ("node_modules/") in {temp}\Templates; project App (folder {temp}\App) with gits
//AppMain, AppLib (CSharp) and AppWeb (Node); every .gitignore of a git is in {gits folder}\{git folder}
[Collection(ConsoleCaptureCollection.Name)]
public sealed class WrongGitignoreFilesListCreatorTests : IDisposable
{
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _projectFolder;
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private readonly string _templatesFolder;

    public WrongGitignoreFilesListCreatorTests()
    {
        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);

        _templatesFolder = Path.Combine(_tempFolder, "Templates");
        Directory.CreateDirectory(_templatesFolder);
        File.WriteAllText(Path.Combine(_templatesFolder, "CSharp.gitignore"), "bin/");
        File.WriteAllText(Path.Combine(_templatesFolder, "Node.gitignore"), "node_modules/");
        _parameters.FolderForGitignoreFiles = _templatesFolder;
        _parameters.GitIgnorePatterns.AddRange(["CSharp", "Node"]);

        _projectFolder = Path.Combine(_tempFolder, "App");
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _projectFolder, GitProjectNames = ["AppMain", "AppLib", "AppWeb"]
        };
        _parameters.Gits["AppMain"] = CreateGit("AppMain", "CSharp");
        _parameters.Gits["AppLib"] = CreateGit("AppLib", "CSharp");
        _parameters.Gits["AppWeb"] = CreateGit("AppWeb", "Node");

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_WhenTemplatesFolderIsNotSpecified_ReportsItAndReturnsEmpty(string? folderForGitignoreFiles)
    {
        // Arrange
        _parameters.FolderForGitignoreFiles = folderForGitignoreFiles;

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        Assert.Contains("supportToolsParameters.FolderForGitignoreFiles is empty", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyErrorLogged("supportToolsParameters.FolderForGitignoreFiles is empty", Times.Once());
    }

    [Fact]
    public void Create_WhenGitignoreFilesMatchTheTemplates_ReturnsEmpty()
    {
        // Arrange
        WriteGitignore(_projectFolder, "AppMain", "bin/");
        WriteGitignore(_projectFolder, "AppLib", "bin/");
        WriteGitignore(_projectFolder, "AppWeb", "node_modules/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        //a project without ScaffoldSeeder gits is skipped for them silently
        Assert.DoesNotContain("ScaffoldSeederProjectName", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //two projects in one folder whose gits share a folder: the project that comes first by name gives the template
    [Fact]
    public void Create_WhenTwoProjectsShareAGitFolder_TakesTheTemplateOfTheFirstProjectByName()
    {
        // Arrange
        _parameters.Gits["SharedNode"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/SharedNode.git", GitProjectFolderName = "Shared",
            GitIgnorePatternName = "Node"
        };
        _parameters.Gits["SharedCSharp"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/SharedCSharp.git", GitProjectFolderName = "Shared",
            GitIgnorePatternName = "CSharp"
        };
        _parameters.Projects.Clear();
        _parameters.Projects["Zeta"] = new ProjectModel { ProjectFolderName = _projectFolder, GitProjectNames = ["SharedNode"] };
        _parameters.Projects["Alpha"] =
            new ProjectModel { ProjectFolderName = _projectFolder, GitProjectNames = ["SharedCSharp"] };
        string shared = WriteGitignore(_projectFolder, "Shared", "dist/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Equal(("bin/", "Shared"), result[shared]);
    }

    [Fact]
    public void Create_WhenGitignoreDiffersOrIsMissing_ReturnsItWithTheTemplateContentAndTheGit()
    {
        // Arrange
        string differing = WriteGitignore(_projectFolder, "AppMain", "obj/");
        WriteGitignore(_projectFolder, "AppWeb", "node_modules/");
        string missing = Path.Combine(_projectFolder, "AppLib", ".gitignore");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(("bin/", "AppMain"), result[differing]);
        Assert.Equal(("bin/", "AppLib"), result[missing]);
    }

    [Fact]
    public void Create_WhenPatternIsNotATemplate_SkipsTheGit()
    {
        // Arrange
        _parameters.GitIgnorePatterns.Remove("Node");
        WriteGitignore(_projectFolder, "AppMain", "bin/");
        WriteGitignore(_projectFolder, "AppLib", "bin/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
    }

    //the missing template is reported once, although two gits use it
    [Fact]
    public void Create_WhenTemplateFileIsMissing_ReportsItOnceAndSkipsItsGits()
    {
        // Arrange
        string templateFileName = Path.Combine(_templatesFolder, "CSharp.gitignore");
        File.Delete(templateFileName);
        WriteGitignore(_projectFolder, "AppWeb", "dist/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Equal([Path.Combine(_projectFolder, "AppWeb", ".gitignore")], result.Keys);
        string expectedError = $"{templateFileName} is not exists";
        Assert.Equal(1, CountOccurrences(_consoleOutput.ToString(), expectedError));
        VerifyErrorLogged(expectedError, Times.Once());
    }

    [Fact]
    public void Create_WhenTwoProjectsUseTheSameTemplate_ReadsItOnceForBoth()
    {
        // Arrange
        string otherFolder = Path.Combine(_tempFolder, "Other");
        _parameters.Projects["Other"] = new ProjectModel { ProjectFolderName = otherFolder, GitProjectNames = ["AppMain"] };
        string appMain = WriteGitignore(_projectFolder, "AppMain", "obj/");
        string otherMain = WriteGitignore(otherFolder, "AppMain", "obj/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Equal(("bin/", "AppMain"), result[appMain]);
        Assert.Equal(("bin/", "AppMain"), result[otherMain]);
    }

    [Fact]
    public void Create_WhenProjectHasNoFolder_SkipsItsMainGits()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel { GitProjectNames = ["AppMain"] };

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        string output = _consoleOutput.ToString();
        Assert.Contains("ProjectFolderName is not specified for project App", output, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectFolderName must be specified", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenProjectHasScaffoldSeederGits_ChecksThemInTheSeederFolder()
    {
        // Arrange
        string seedersFolder = Path.Combine(_tempFolder, "Seeders");
        _parameters.ScaffoldSeedersWorkFolder = seedersFolder;
        _parameters.Gits["AppSeeder"] = CreateGit("AppSeeder", "CSharp");
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _projectFolder,
            ScaffoldSeederProjectName = "App",
            ScaffoldSeederGitProjectNames = ["AppSeeder"]
        };
        string seederGitignore =
            WriteGitignore(Path.Combine(seedersFolder, "App", "AppScaffoldSeeder"), "AppSeeder", "obj/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Equal([seederGitignore], result.Keys);
    }

    //without a seeders work folder the seeder gits have no folder to check
    [Fact]
    public void Create_WhenSeedersWorkFolderIsNotSpecified_SkipsTheSeederGits()
    {
        // Arrange
        _parameters.Gits["AppSeeder"] = CreateGit("AppSeeder", "CSharp");
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _projectFolder,
            ScaffoldSeederProjectName = "App",
            ScaffoldSeederGitProjectNames = ["AppSeeder"]
        };

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Create_WhenSpaRecordIsMissing_ReportsErrorAndChecksTheOtherGits()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectFolderName = _projectFolder, SpaProjectName = "FrontApp", GitProjectNames = ["AppMain", "AppSpa"]
        };
        _parameters.Gits["AppSpa"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/AppSpa.git",
            GitProjectFolderName = $@"{GitDataModel.SpaProjectFolderRelativePathName}\ClientApp",
            GitIgnorePatternName = "Node"
        };
        string appMain = WriteGitignore(_projectFolder, "AppMain", "obj/");

        // Act
        Dictionary<string, (string, string)> result = CreateSut().Create();

        // Assert
        Assert.Contains(SpaProjectFolderHelper.GetMissingSpaProjectMessage("App", "FrontApp"),
            _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal([appMain], result.Keys);
    }

    private WrongGitignoreFilesListCreator CreateSut()
    {
        return new WrongGitignoreFilesListCreator(_logger.Object, _parametersManager.Object, true);
    }

    private static string WriteGitignore(string gitsFolder, string gitFolderName, string content)
    {
        string gitFolder = Path.Combine(gitsFolder, gitFolderName);
        Directory.CreateDirectory(gitFolder);
        string fileName = Path.Combine(gitFolder, ".gitignore");
        File.WriteAllText(fileName, content);
        return fileName;
    }

    private static GitDataModel CreateGit(string gitName, string gitIgnorePatternName)
    {
        return new GitDataModel
        {
            GitProjectAddress = $"git@github.com:x/{gitName}.git",
            GitProjectFolderName = gitName,
            GitIgnorePatternName = gitIgnorePatternName
        };
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }

    private void VerifyErrorLogged(string message, Times times)
    {
        _logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((value, _) => value.ToString() == message), It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
    }
}
