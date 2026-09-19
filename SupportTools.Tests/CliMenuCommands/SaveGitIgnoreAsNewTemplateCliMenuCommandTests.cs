using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using LibGitData;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

public sealed class SaveGitIgnoreAsNewTemplateCliMenuCommandTests : IDisposable
{
    private const string ProjectName = "TestProject";
    private const string GitProjectName = "MyGit";
    private const string MenuName = "Save .gitignore as New Template";
    private const string NewTemplateName = "NewTemplate";

    //mixed line endings, non-ASCII text and no trailing newline: the template must be a byte-exact copy
    private static readonly byte[] SourceGitIgnoreContent = "bin/\r\nobj/\n# ქართული\n*.user"u8.ToArray();

    private readonly Queue<bool> _boolAnswers = new();
    private readonly List<(string FieldName, bool DefaultValue)> _boolPrompts = [];
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _rootFolder;
    private readonly string _sourceGitIgnoreFileName;
    private readonly string _templatesFolder;
    private readonly Queue<Func<string?, string?>> _textAnswers = new();
    private readonly List<(string FieldName, string? DefaultValue)> _textPrompts = [];

    public SaveGitIgnoreAsNewTemplateCliMenuCommandTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        string gitsFolder = Path.Combine(_rootFolder, "gits");
        _templatesFolder = Path.Combine(_rootFolder, "templates");
        _sourceGitIgnoreFileName = Path.Combine(gitsFolder, GitProjectName, ".gitignore");
        Directory.CreateDirectory(Path.Combine(gitsFolder, GitProjectName));
        Directory.CreateDirectory(_templatesFolder);
        File.WriteAllBytes(_sourceGitIgnoreFileName, SourceGitIgnoreContent);

        _parameters = new SupportToolsParameters
        {
            FolderForGitignoreFiles = _templatesFolder,
            GitIgnorePatterns = { "CSharp", "React" },
            Projects =
            {
                [ProjectName] = new ProjectModel
                {
                    ProjectFolderName = gitsFolder, GitProjectNames = { GitProjectName }
                }
            },
            Gits =
            {
                [GitProjectName] = new GitDataModel
                {
                    GitProjectAddress = "MyGitRemoteAddress",
                    GitProjectFolderName = GitProjectName,
                    GitIgnorePatternName = "CSharp"
                }
            }
        };

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        SetupSaveResult(true);

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        Directory.Delete(_rootFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        SaveGitIgnoreAsNewTemplateCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal(MenuName, sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
    }

    [Fact]
    public void PublicConstructor_WhenCreated_SetsMenuName()
    {
        // Act
        var sut = new SaveGitIgnoreAsNewTemplateCliMenuCommand(_logger.Object, _parametersManager.Object,
            ProjectName, GitProjectName, EGitCol.Main);

        // Assert
        Assert.Equal(MenuName, sut.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunBody_WhenFolderForGitignoreFilesIsNotSpecified_ReturnsFalseWithoutAskingName(
        string? folderForGitignoreFiles)
    {
        // Arrange
        _parameters.FolderForGitignoreFiles = folderForGitignoreFiles;

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains("FolderForGitignoreFiles is not specified", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenGitIsNotInProject_ReturnsFalseWithoutAskingName()
    {
        // Arrange
        _parameters.Projects[ProjectName].GitProjectNames.Clear();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
    }

    [Fact]
    public async Task RunBody_WhenGitHasNoGitIgnorePatternName_ReportsItAndReturnsFalse()
    {
        // Arrange
        _parameters.Gits[GitProjectName].GitIgnorePatternName = null;

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains($"GitIgnorePatternName is empty for Git Repo with key {GitProjectName}", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenGitFolderHasNoGitIgnoreFile_ReturnsFalseWithoutAskingName()
    {
        // Arrange
        File.Delete(_sourceGitIgnoreFileName);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains($"File {_sourceGitIgnoreFileName} does not exist", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenAskingName_OffersGitProjectNameAsDefault()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        (string fieldName, string? defaultValue) = Assert.Single(_textPrompts);
        Assert.Equal("New .gitignore Template Name", fieldName);
        Assert.Equal(GitProjectName, defaultValue);
    }

    [Fact]
    public async Task RunBody_WhenDefaultNameAccepted_CopiesGitIgnoreToTemplateFile()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(SourceGitIgnoreContent, await File.ReadAllBytesAsync(TemplateFileName(GitProjectName)));
    }

    [Fact]
    public async Task RunBody_WhenDefaultNameAccepted_AddsTemplateNameToGitIgnorePatterns()
    {
        // Arrange
        string[] expected = ["CSharp", "React", GitProjectName];
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, _parameters.GitIgnorePatterns);
    }

    [Fact]
    public async Task RunBody_WhenTemplateCreated_SavesParametersWithMessage()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        _parametersManager.Verify(
            x => x.Save(_parameters, $".gitignore template {GitProjectName} created", null,
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_boolPrompts);
    }

    [Fact]
    public async Task RunBody_WhenTemplateCreated_KeepsGitIgnorePatternNameOfGit()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal("CSharp", _parameters.Gits[GitProjectName].GitIgnorePatternName);
    }

    [Fact]
    public async Task RunBody_WhenSavingParametersFails_ReturnsFalse()
    {
        // Arrange
        SetupSaveResult(false);
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunBody_WhenNameIsBlank_ReportsErrorAndAsksAgain(string? blankName)
    {
        // Arrange
        AnswerText(blankName);
        AnswerText(NewTemplateName);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(2, _textPrompts.Count);
        Assert.Contains("Template name is empty", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenNameHasSurroundingWhitespace_UsesTrimmedName()
    {
        // Arrange
        AnswerText($"  {NewTemplateName}  ");

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains(NewTemplateName, _parameters.GitIgnorePatterns);
        Assert.True(File.Exists(TemplateFileName(NewTemplateName)));
    }

    [Theory]
    [InlineData("CSharp")]
    [InlineData("csharp")]
    [InlineData("REACT")]
    public async Task RunBody_WhenNameAlreadyUsedIgnoringCase_ReportsErrorAndAsksAgain(string usedName)
    {
        // Arrange
        string[] expected = ["CSharp", "React", NewTemplateName];
        AnswerText(usedName);
        AnswerText(NewTemplateName);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains($"Template with name {usedName} already exists", ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(expected, _parameters.GitIgnorePatterns);
    }

    [Fact]
    public async Task RunBody_WhenNameAlreadyUsedIgnoringCase_KeepsExistingTemplateFile()
    {
        // Arrange
        string existingTemplateFileName = TemplateFileName("CSharp");
        await File.WriteAllTextAsync(existingTemplateFileName, "existing template");
        AnswerText("csharp");
        AnswerText(NewTemplateName);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal("existing template", await File.ReadAllTextAsync(existingTemplateFileName));
    }

    //'|' is invalid in Windows file names; at index 0 it also guards the ">= 0" boundary of the check
    [Theory]
    [InlineData("Bad/Name")]
    [InlineData("|BadName")]
    public async Task RunBody_WhenNameHasInvalidFileNameCharacters_ReportsErrorAndAsksAgain(string invalidName)
    {
        // Arrange
        string[] expected = ["CSharp", "React", NewTemplateName];
        AnswerText(invalidName);
        AnswerText(NewTemplateName);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains($"Template name {invalidName} contains invalid file name characters", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Equal(expected, _parameters.GitIgnorePatterns);
    }

    [Fact]
    public async Task RunBody_WhenNameInputIsEscaped_PropagatesEscapeWithoutChanges()
    {
        // Arrange
        string[] expected = ["CSharp", "React"];
        _textAnswers.Enqueue(_ => throw new DataInputEscapeException("Escape"));

        // Act
        await Assert.ThrowsAsync<DataInputEscapeException>(() => InvokeRunBody(CreateSut()));

        // Assert
        Assert.Equal(expected, _parameters.GitIgnorePatterns);
    }

    [Fact]
    public async Task RunBody_WhenTemplateFileAlreadyExists_AsksToOverwriteWithNoAsDefault()
    {
        // Arrange
        string templateFileName = await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(false);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        (string fieldName, bool defaultValue) = Assert.Single(_boolPrompts);
        Assert.Equal($"File {templateFileName} exists, overwrite?", fieldName);
        Assert.False(defaultValue);
    }

    [Fact]
    public async Task RunBody_WhenOverwriteDeclined_ReturnsFalseAndKeepsExistingFile()
    {
        // Arrange
        string templateFileName = await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(false);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Equal("orphan template", await File.ReadAllTextAsync(templateFileName));
    }

    [Fact]
    public async Task RunBody_WhenOverwriteDeclined_DoesNotChangeParameters()
    {
        // Arrange
        string[] expected = ["CSharp", "React"];
        await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(false);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, _parameters.GitIgnorePatterns);
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunBody_WhenOverwriteConfirmed_ReplacesFileWithGitIgnoreCopy()
    {
        // Arrange
        string templateFileName = await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(true);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(SourceGitIgnoreContent, await File.ReadAllBytesAsync(templateFileName));
    }

    [Fact]
    public async Task RunBody_WhenOverwriteConfirmed_AddsTemplateNameToGitIgnorePatterns()
    {
        // Arrange
        string[] expected = ["CSharp", "React", "Orphan"];
        await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(true);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, _parameters.GitIgnorePatterns);
    }

    [Fact]
    public async Task RunBody_WhenTemplatesFolderDoesNotExist_CreatesItWithTemplateFile()
    {
        // Arrange
        string missingTemplatesFolder = Path.Combine(_rootFolder, "missing", "templates");
        _parameters.FolderForGitignoreFiles = missingTemplatesFolder;
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(SourceGitIgnoreContent,
            await File.ReadAllBytesAsync(Path.Combine(missingTemplatesFolder, $"{GitProjectName}.gitignore")));
    }

    [Fact]
    public async Task RunBody_WhenTemplatesFolderCannotBeCreated_ReportsErrorAndReturnsFalse()
    {
        // Arrange
        //a folder cannot be created below an existing file
        string blockingFileName = Path.Combine(_rootFolder, "blocking-file");
        await File.WriteAllTextAsync(blockingFileName, string.Empty);
        _parameters.FolderForGitignoreFiles = Path.Combine(blockingFileName, "templates");
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("[ERROR]", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenGitIsScaffoldSeederGit_CopiesGitIgnoreFromScaffoldSeederFolder()
    {
        // Arrange
        //ScaffoldSeeder gits live in {ScaffoldSeedersWorkFolder}\{ScaffoldSeederProjectName}\{ScaffoldSeederProjectName}ScaffoldSeeder
        byte[] seederGitIgnoreContent = "seeder-only/"u8.ToArray();
        string scaffoldSeedersWorkFolder = Path.Combine(_rootFolder, "seeders");
        string seederGitFolder = Path.Combine(scaffoldSeedersWorkFolder, "Seeder", "SeederScaffoldSeeder", GitProjectName);
        Directory.CreateDirectory(seederGitFolder);
        await File.WriteAllBytesAsync(Path.Combine(seederGitFolder, ".gitignore"), seederGitIgnoreContent);
        _parameters.ScaffoldSeedersWorkFolder = scaffoldSeedersWorkFolder;
        ProjectModel project = _parameters.Projects[ProjectName];
        project.ScaffoldSeederProjectName = "Seeder";
        project.ScaffoldSeederGitProjectNames = [GitProjectName];
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut(EGitCol.ScaffoldSeed));

        // Assert
        Assert.True(result);
        Assert.Equal(seederGitIgnoreContent, await File.ReadAllBytesAsync(TemplateFileName(GitProjectName)));
    }

    //RunBody is protected, and its result is not observable through Run(): success and failure both reload the menu
    private static async Task<bool> InvokeRunBody(SaveGitIgnoreAsNewTemplateCliMenuCommand sut)
    {
        MethodInfo runBody = typeof(SaveGitIgnoreAsNewTemplateCliMenuCommand).GetMethod("RunBody",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (ValueTask<bool>)runBody.Invoke(sut, [CancellationToken.None])!;
    }

    private SaveGitIgnoreAsNewTemplateCliMenuCommand CreateSut(EGitCol gitCol = EGitCol.Main)
    {
        return new SaveGitIgnoreAsNewTemplateCliMenuCommand(_logger.Object, _parametersManager.Object, ProjectName,
            GitProjectName, gitCol, InputText, InputBool);
    }

    private string? InputText(string fieldName, string? defaultValue)
    {
        _textPrompts.Add((fieldName, defaultValue));
        return _textAnswers.Dequeue()(defaultValue);
    }

    private bool InputBool(string fieldName, bool defaultValue)
    {
        _boolPrompts.Add((fieldName, defaultValue));
        return _boolAnswers.Dequeue();
    }

    //Enter on an empty prompt returns the offered default
    private void AnswerWithDefault()
    {
        _textAnswers.Enqueue(defaultValue => defaultValue);
    }

    private void AnswerText(string? text)
    {
        _textAnswers.Enqueue(_ => text);
    }

    private void SetupSaveResult(bool result)
    {
        _parametersManager
            .Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(result);
    }

    private async Task<string> CreateOrphanTemplateFile()
    {
        string templateFileName = TemplateFileName("Orphan");
        await File.WriteAllTextAsync(templateFileName, "orphan template");
        return templateFileName;
    }

    private string TemplateFileName(string templateName)
    {
        return Path.Combine(_templatesFolder, $"{templateName}.gitignore");
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }
}
