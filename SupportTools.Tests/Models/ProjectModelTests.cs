using System.IO;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Models;

public sealed class ProjectModelTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EditorConfigFileName_WhenSolutionFileNameIsNotSpecified_ReturnsNull(string? solutionFileName)
    {
        // Arrange
        var sut = new ProjectModel { SolutionFileName = solutionFileName };

        // Act
        string? result = sut.EditorConfigFileName();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EditorConfigFileName_WhenSolutionFileNameHasNoFolder_ReturnsNull()
    {
        // Arrange
        var sut = new ProjectModel { SolutionFileName = "MyProject.slnx" };

        // Act
        string? result = sut.EditorConfigFileName();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EditorConfigFileName_WhenSolutionFileNameIsSpecified_ReturnsFileBesideSolution()
    {
        // Arrange
        string solutionFolder = Path.Combine(Path.GetTempPath(), "MyProject", "MyProject");
        var sut = new ProjectModel { SolutionFileName = Path.Combine(solutionFolder, "MyProject.slnx") };

        // Act
        string? result = sut.EditorConfigFileName();

        // Assert
        Assert.Equal(Path.Combine(solutionFolder, ".editorconfig"), result);
    }
}
