using System;
using System.Collections.Generic;
using System.IO;
using SupportTools.Tools;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Tools;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class WrongEditorConfigFilesListCreatorTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenFolderForEditorConfigFilesIsNotSpecified_ReportsErrorAndReturnsEmpty(
        string? folderForEditorConfigFiles)
    {
        // Arrange
        _env.Parameters.FolderForEditorConfigFiles = folderForEditorConfigFiles;
        File.WriteAllText(_env.EditorConfigFileName, "changed");

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        Assert.Contains("FolderForEditorConfigFiles is empty", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenEditorConfigFileMatchesTemplate_ReturnsEmpty()
    {
        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Create_WhenEditorConfigFileDiffersFromTemplate_ReturnsItWithTemplateContent()
    {
        // Arrange
        File.WriteAllText(_env.EditorConfigFileName, "changed");

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        (string fileName, string templateContent) = Assert.Single(result);
        Assert.Equal(_env.EditorConfigFileName, fileName);
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent, templateContent);
    }

    [Fact]
    public void Create_WhenSolutionFolderHasNoEditorConfigFile_ReturnsIt()
    {
        // Arrange
        File.Delete(_env.EditorConfigFileName);

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Equal([_env.EditorConfigFileName], result.Keys);
    }

    //EditorConfigPatternName is optional: projects without it are not checked at all
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenProjectHasNoEditorConfigPatternName_SkipsProject(string? editorConfigPatternName)
    {
        // Arrange
        _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName].EditorConfigPatternName =
            editorConfigPatternName;
        File.Delete(_env.EditorConfigFileName);

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        Assert.DoesNotContain("[ERROR]", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WhenPatternIsNotInEditorConfigPatterns_SkipsProject()
    {
        // Arrange
        _env.Parameters.EditorConfigPatterns.Clear();
        File.WriteAllText(_env.EditorConfigFileName, "changed");

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Create_WhenProjectHasNoSolutionFile_SkipsProject()
    {
        // Arrange
        _env.Parameters.Projects["NoSolution"] = new ProjectModel
        {
            ProjectFolderName = _env.RootFolder, EditorConfigPatternName = EditorConfigTestEnvironment.PatternName
        };

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        Assert.DoesNotContain("[ERROR]", _env.ConsoleText(), StringComparison.Ordinal);
    }

    //a registered project that is not cloned on this machine must not be reported (Update could not write it anyway)
    [Fact]
    public void Create_WhenSolutionFolderDoesNotExist_SkipsProject()
    {
        // Arrange
        string editorConfigFileName =
            _env.AddProject("NotCloned", EditorConfigTestEnvironment.PatternName, "changed");
        Directory.Delete(Path.GetDirectoryName(editorConfigFileName)!, true);

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Create_WhenTemplateFileDoesNotExist_ReportsErrorOnceAndSkipsProjects()
    {
        // Arrange
        File.Delete(_env.TemplateFileName);
        File.WriteAllText(_env.EditorConfigFileName, "changed");
        _env.AddProject("Second", EditorConfigTestEnvironment.PatternName, "changed");
        string expectedError = $"{_env.TemplateFileName} is not exists";

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Empty(result);
        string consoleText = _env.ConsoleText();
        int firstIndex = consoleText.IndexOf(expectedError, StringComparison.Ordinal);
        Assert.True(firstIndex >= 0);
        Assert.Equal(-1, consoleText.IndexOf(expectedError, firstIndex + 1, StringComparison.Ordinal));
    }

    [Fact]
    public void Create_WhenProjectsUseDifferentPatterns_ChecksEachProjectAgainstItsOwnTemplate()
    {
        // Arrange
        //the React project holds the CSharp content, but it is bound to the React template
        const string reactTemplateContent = "root = true\r\n\r\n[*.ts]\r\nindent_size = 2\r\n";
        File.WriteAllText(Path.Combine(_env.TemplatesFolder, "React.editorconfig"), reactTemplateContent);
        _env.Parameters.EditorConfigPatterns.Add("React");
        string reactEditorConfigFileName =
            _env.AddProject("ReactProject", "React", EditorConfigTestEnvironment.TemplateContent);

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        (string fileName, string templateContent) = Assert.Single(result);
        Assert.Equal(reactEditorConfigFileName, fileName);
        Assert.Equal(reactTemplateContent, templateContent);
    }

    [Fact]
    public void Create_WhenSeveralProjectsAreWrong_ReturnsThemOrderedByProjectName()
    {
        // Arrange
        File.WriteAllText(_env.EditorConfigFileName, "changed");
        string firstFileName = _env.AddProject("AProject", EditorConfigTestEnvironment.PatternName, "changed");
        string lastFileName = _env.AddProject("ZProject", EditorConfigTestEnvironment.PatternName, null);

        // Act
        Dictionary<string, string> result = CreateSut().Create();

        // Assert
        Assert.Equal([firstFileName, _env.EditorConfigFileName, lastFileName], result.Keys);
    }

    private WrongEditorConfigFilesListCreator CreateSut()
    {
        return new WrongEditorConfigFilesListCreator(null, _env.ParametersManager.Object);
    }
}
