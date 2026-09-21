using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SaveEditorConfigAsNewTemplateCliMenuCommandTests : IDisposable
{
    private const string ProjectName = EditorConfigTestEnvironment.ProjectName;
    private const string MenuName = "Save .editorconfig as New Template";
    private const string NewTemplateName = "NewTemplate";

    //mixed line endings, non-ASCII text and no trailing newline: the template must be a byte-exact copy
    private static readonly byte[] SourceEditorConfigContent =
        [.. "root = true\r\n[*.cs]\n# ქართული\nindent_size = 4"u8];

    private readonly Queue<bool> _boolAnswers = new();
    private readonly List<(string FieldName, bool DefaultValue)> _boolPrompts = [];
    private readonly EditorConfigTestEnvironment _env = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly Queue<Func<string?, string?>> _textAnswers = new();
    private readonly List<(string FieldName, string? DefaultValue)> _textPrompts = [];

    public SaveEditorConfigAsNewTemplateCliMenuCommandTests()
    {
        _env.Parameters.EditorConfigPatterns.Add("React");
        File.WriteAllBytes(_env.EditorConfigFileName, SourceEditorConfigContent);
    }

    private SupportToolsParameters Parameters => _env.Parameters;

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        SaveEditorConfigAsNewTemplateCliMenuCommand sut = CreateSut();

        // Assert
        Assert.Equal(MenuName, sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodyFail);
        Assert.Equal(ProjectName, sut.ParentMenuName);
    }

    [Fact]
    public void PublicConstructor_WhenCreated_SetsMenuName()
    {
        // Act
        var sut = new SaveEditorConfigAsNewTemplateCliMenuCommand(_logger.Object, _env.ParametersManager.Object,
            ProjectName);

        // Assert
        Assert.Equal(MenuName, sut.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunBody_WhenFolderForEditorConfigFilesIsNotSpecified_ReturnsFalseWithoutAskingName(
        string? folderForEditorConfigFiles)
    {
        // Arrange
        Parameters.FolderForEditorConfigFiles = folderForEditorConfigFiles;

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains("FolderForEditorConfigFiles is not specified", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenProjectIsNotFound_ReportsItAndReturnsFalse()
    {
        // Arrange
        Parameters.Projects.Clear();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains($"Project {ProjectName} does not found", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RunBody_WhenProjectHasNoSolutionFile_ReportsItAndReturnsFalse(string? solutionFileName)
    {
        // Arrange
        Parameters.Projects[ProjectName] = new ProjectModel { SolutionFileName = solutionFileName };

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains($"Project {ProjectName} does not have a solution file", _env.ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenSolutionFolderHasNoEditorConfigFile_ReturnsFalseWithoutAskingName()
    {
        // Arrange
        File.Delete(_env.EditorConfigFileName);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Empty(_textPrompts);
        Assert.Contains($"File {_env.EditorConfigFileName} does not exist", _env.ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenAskingName_OffersProjectNameAsDefault()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        (string fieldName, string? defaultValue) = Assert.Single(_textPrompts);
        Assert.Equal("New .editorconfig Template Name", fieldName);
        Assert.Equal(ProjectName, defaultValue);
    }

    [Fact]
    public async Task RunBody_WhenDefaultNameAccepted_CopiesEditorConfigToTemplateFile()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(SourceEditorConfigContent, await File.ReadAllBytesAsync(TemplateFileName(ProjectName)));
    }

    [Fact]
    public async Task RunBody_WhenDefaultNameAccepted_AddsTemplateNameToEditorConfigPatterns()
    {
        // Arrange
        string[] expected = ["CSharp", "React", ProjectName];
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, Parameters.EditorConfigPatterns);
    }

    [Fact]
    public async Task RunBody_WhenTemplateCreated_SavesParametersWithMessage()
    {
        // Arrange
        AnswerWithDefault();

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        _env.ParametersManager.Verify(
            x => x.Save(Parameters, $".editorconfig template {ProjectName} created", null,
                It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_boolPrompts);
    }

    [Fact]
    public async Task RunBody_WhenTemplateCreated_KeepsEditorConfigPatternNameOfProject()
    {
        // Arrange
        AnswerText(NewTemplateName);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(EditorConfigTestEnvironment.PatternName, Parameters.Projects[ProjectName].EditorConfigPatternName);
    }

    //EditorConfigPatternName is optional: a project without it is still a valid template source
    [Fact]
    public async Task RunBody_WhenProjectHasNoEditorConfigPatternName_CreatesTemplateAndLeavesItUnset()
    {
        // Arrange
        Parameters.Projects[ProjectName].EditorConfigPatternName = null;
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Null(Parameters.Projects[ProjectName].EditorConfigPatternName);
    }

    [Fact]
    public async Task RunBody_WhenSavingParametersFails_ReturnsFalse()
    {
        // Arrange
        _env.ParametersManager
            .Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(false);
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
        Assert.Contains("Template name is empty", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenNameHasSurroundingWhitespace_UsesTrimmedName()
    {
        // Arrange
        AnswerText($"  {NewTemplateName}  ");

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Contains(NewTemplateName, Parameters.EditorConfigPatterns);
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
        Assert.Contains($"Template with name {usedName} already exists", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Equal(expected, Parameters.EditorConfigPatterns);
    }

    [Fact]
    public async Task RunBody_WhenNameAlreadyUsedIgnoringCase_KeepsExistingTemplateFile()
    {
        // Arrange
        AnswerText("csharp");
        AnswerText(NewTemplateName);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent, await File.ReadAllTextAsync(_env.TemplateFileName));
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
        Assert.Contains($"Template name {invalidName} contains invalid file name characters", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Equal(expected, Parameters.EditorConfigPatterns);
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
        Assert.Equal(expected, Parameters.EditorConfigPatterns);
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
        Assert.Equal(expected, Parameters.EditorConfigPatterns);
        _env.ParametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunBody_WhenOverwriteConfirmed_ReplacesFileWithEditorConfigCopy()
    {
        // Arrange
        string templateFileName = await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(true);

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(SourceEditorConfigContent, await File.ReadAllBytesAsync(templateFileName));
    }

    [Fact]
    public async Task RunBody_WhenOverwriteConfirmed_AddsTemplateNameToEditorConfigPatterns()
    {
        // Arrange
        string[] expected = ["CSharp", "React", "Orphan"];
        await CreateOrphanTemplateFile();
        AnswerText("Orphan");
        _boolAnswers.Enqueue(true);

        // Act
        await InvokeRunBody(CreateSut());

        // Assert
        Assert.Equal(expected, Parameters.EditorConfigPatterns);
    }

    [Fact]
    public async Task RunBody_WhenTemplatesFolderDoesNotExist_CreatesItWithTemplateFile()
    {
        // Arrange
        string missingTemplatesFolder = Path.Combine(_env.RootFolder, "missing", "templates");
        Parameters.FolderForEditorConfigFiles = missingTemplatesFolder;
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(SourceEditorConfigContent,
            await File.ReadAllBytesAsync(Path.Combine(missingTemplatesFolder, $"{ProjectName}.editorconfig")));
    }

    [Fact]
    public async Task RunBody_WhenTemplatesFolderCannotBeCreated_ReportsErrorAndReturnsFalse()
    {
        // Arrange
        //a folder cannot be created below an existing file
        string blockingFileName = Path.Combine(_env.RootFolder, "blocking-file");
        await File.WriteAllTextAsync(blockingFileName, string.Empty);
        Parameters.FolderForEditorConfigFiles = Path.Combine(blockingFileName, "templates");
        AnswerWithDefault();

        // Act
        bool result = await InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("[ERROR]", _env.ConsoleText(), StringComparison.Ordinal);
    }

    //RunBody is protected, and its result is not observable through Run(): success and failure both reload the menu
    private static async Task<bool> InvokeRunBody(SaveEditorConfigAsNewTemplateCliMenuCommand sut)
    {
        MethodInfo runBody = typeof(SaveEditorConfigAsNewTemplateCliMenuCommand).GetMethod("RunBody",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await (ValueTask<bool>)runBody.Invoke(sut, [CancellationToken.None])!;
    }

    private SaveEditorConfigAsNewTemplateCliMenuCommand CreateSut()
    {
        return new SaveEditorConfigAsNewTemplateCliMenuCommand(_logger.Object, _env.ParametersManager.Object,
            ProjectName, InputText, InputBool);
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

    private async Task<string> CreateOrphanTemplateFile()
    {
        string templateFileName = TemplateFileName("Orphan");
        await File.WriteAllTextAsync(templateFileName, "orphan template");
        return templateFileName;
    }

    private string TemplateFileName(string templateName)
    {
        return Path.Combine(_env.TemplatesFolder, $"{templateName}.editorconfig");
    }
}
