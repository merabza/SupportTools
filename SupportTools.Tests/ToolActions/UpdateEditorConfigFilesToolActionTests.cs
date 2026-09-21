using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SupportTools.ToolActions;
using SupportTools.Tools;
using Xunit;

namespace SupportTools.Tests.ToolActions;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class UpdateEditorConfigFilesToolActionTests : IDisposable
{
    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Fact]
    public async Task Run_WhenEditorConfigFileDiffersFromTemplate_OverwritesItWithTemplateContent()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(await File.ReadAllBytesAsync(_env.TemplateFileName),
            await File.ReadAllBytesAsync(_env.EditorConfigFileName));
    }

    [Fact]
    public async Task Run_WhenSolutionFolderHasNoEditorConfigFile_CreatesItFromTemplate()
    {
        // Arrange
        File.Delete(_env.EditorConfigFileName);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent,
            await File.ReadAllTextAsync(_env.EditorConfigFileName));
    }

    [Fact]
    public async Task Run_WhenSeveralProjectsAreWrong_UpdatesAllOfThem()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");
        string secondFileName = _env.AddProject("Second", EditorConfigTestEnvironment.PatternName, "changed too");

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent,
            await File.ReadAllTextAsync(_env.EditorConfigFileName));
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent, await File.ReadAllTextAsync(secondFileName));
        Assert.Empty(new WrongEditorConfigFilesListCreator(null, _env.ParametersManager.Object).Create());
    }

    [Fact]
    public async Task Run_WhenFilesAreUpdated_ReportsEachUpdatedFile()
    {
        // Arrange
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");
        string secondFileName = _env.AddProject("Second", EditorConfigTestEnvironment.PatternName, null);

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        string consoleText = _env.ConsoleText();
        Assert.Contains("Update wrong .editorconfig files", consoleText, StringComparison.Ordinal);
        Assert.Contains($"Update {_env.EditorConfigFileName}", consoleText, StringComparison.Ordinal);
        Assert.Contains($"Update {secondFileName}", consoleText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_WhenProjectHasNoEditorConfigPatternName_LeavesItsFileUntouched()
    {
        // Arrange
        _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName].EditorConfigPatternName = null;
        await File.WriteAllTextAsync(_env.EditorConfigFileName, "changed");

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal("changed", await File.ReadAllTextAsync(_env.EditorConfigFileName));
        Assert.Contains("--wrong .editorconfig files are not found", _env.ConsoleText(), StringComparison.Ordinal);
    }

    private UpdateEditorConfigFilesToolAction CreateSut()
    {
        return new UpdateEditorConfigFilesToolAction(new Mock<ILogger>().Object, _env.ParametersManager.Object, true);
    }
}
