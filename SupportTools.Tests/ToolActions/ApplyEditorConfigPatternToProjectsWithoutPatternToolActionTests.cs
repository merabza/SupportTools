using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.ToolActions;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.ToolActions;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class ApplyEditorConfigPatternToProjectsWithoutPatternToolActionTests : IDisposable
{
    private const string AppliedPatternName = "Applied";

    private readonly EditorConfigTestEnvironment _env = new();

    public void Dispose()
    {
        _env.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Run_WhenProjectHasNoEditorConfigPatternName_AppliesPatternToIt(string? editorConfigPatternName)
    {
        // Arrange
        _env.AddProject("NoPattern", editorConfigPatternName, null);

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(AppliedPatternName, _env.Parameters.Projects["NoPattern"].EditorConfigPatternName);
    }

    [Fact]
    public async Task Run_WhenProjectAlreadyHasEditorConfigPatternName_KeepsIt()
    {
        // Arrange
        _env.AddProject("NoPattern", null, null);

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.Equal(EditorConfigTestEnvironment.PatternName,
            _env.Parameters.Projects[EditorConfigTestEnvironment.ProjectName].EditorConfigPatternName);
    }

    //a project without a solution file has no .editorconfig of its own, so there is nothing to bind the pattern to
    [Fact]
    public async Task Run_WhenProjectHasNoSolutionFile_LeavesItWithoutPattern()
    {
        // Arrange
        _env.Parameters.Projects["NoSolution"] = new ProjectModel { ProjectFolderName = _env.RootFolder };
        _env.AddProject("NoPattern", null, null);

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.Null(_env.Parameters.Projects["NoSolution"].EditorConfigPatternName);
    }

    [Fact]
    public async Task Run_WhenPatternApplied_SavesParametersOnce()
    {
        // Arrange
        _env.AddProject("NoPattern", null, null);
        _env.AddProject("AnotherNoPattern", null, null);

        // Act
        await CreateSut().Run(CancellationToken.None);

        // Assert
        _env.ParametersManager.Verify(
            x => x.Save(_env.Parameters, "EditorConfigPatternNames applied success", null,
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_WhenNoProjectNeedsPattern_WarnsAndDoesNotSave()
    {
        // Arrange
        _env.Parameters.Projects["NoSolution"] = new ProjectModel { ProjectFolderName = _env.RootFolder };

        // Act
        bool result = await CreateSut().Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Contains("All Projects already have EditorConfigPatternNames", _env.ConsoleText(),
            StringComparison.Ordinal);
        _env.ParametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    private ApplyEditorConfigPatternToProjectsWithoutPatternToolAction CreateSut()
    {
        return new ApplyEditorConfigPatternToProjectsWithoutPatternToolAction(new Mock<ILogger>().Object,
            AppliedPatternName, _env.ParametersManager.Object);
    }
}
