using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Models;

//D3: a project is local when its ProjectFolderName exists on this computer
public sealed class ProjectPresenceTests : IDisposable
{
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), $"ProjectPresenceTests_{Guid.NewGuid():N}");

    public ProjectPresenceTests()
    {
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void IsLocal_WhenProjectFolderExists_ReturnsTrue()
    {
        // Arrange
        var project = new ProjectModel { ProjectFolderName = _tempFolder };

        // Act
        bool result = ProjectPresence.IsLocal(project);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocal_WhenProjectFolderDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var project = new ProjectModel { ProjectFolderName = Path.Combine(_tempFolder, "Missing") };

        // Act
        bool result = ProjectPresence.IsLocal(project);

        // Assert
        Assert.False(result);
    }

    //a file with the folder's name is not a project folder
    [Fact]
    public void IsLocal_WhenProjectFolderNameIsAFile_ReturnsFalse()
    {
        // Arrange
        string fileName = Path.Combine(_tempFolder, "file.txt");
        File.WriteAllText(fileName, "x");
        var project = new ProjectModel { ProjectFolderName = fileName };

        // Act
        bool result = ProjectPresence.IsLocal(project);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsLocal_WhenProjectFolderNameIsEmpty_ReturnsFalse(string? projectFolderName)
    {
        // Arrange
        var project = new ProjectModel { ProjectFolderName = projectFolderName };

        // Act
        bool result = ProjectPresence.IsLocal(project);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Filter_WhenRemoteProjectsAreExcluded_KeepsOnlyLocalProjectsInOrder()
    {
        // Arrange
        List<KeyValuePair<string, ProjectModel>> projects = CreateProjects();

        // Act
        List<string> result = [.. ProjectPresence.Filter(projects, false).Select(x => x.Key)];

        // Assert
        Assert.Equal(["LocalA", "LocalB"], result);
    }

    [Fact]
    public void Filter_WhenRemoteProjectsAreIncluded_KeepsEveryProject()
    {
        // Arrange
        List<KeyValuePair<string, ProjectModel>> projects = CreateProjects();

        // Act
        List<string> result = [.. ProjectPresence.Filter(projects, true).Select(x => x.Key)];

        // Assert
        Assert.Equal(["LocalA", "Remote", "LocalB", "NoFolder"], result);
    }

    private List<KeyValuePair<string, ProjectModel>> CreateProjects()
    {
        return
        [
            new KeyValuePair<string, ProjectModel>("LocalA", new ProjectModel { ProjectFolderName = _tempFolder }),
            new KeyValuePair<string, ProjectModel>("Remote",
                new ProjectModel { ProjectFolderName = Path.Combine(_tempFolder, "Missing") }),
            new KeyValuePair<string, ProjectModel>("LocalB", new ProjectModel { ProjectFolderName = _tempFolder }),
            new KeyValuePair<string, ProjectModel>("NoFolder", new ProjectModel())
        ];
    }
}
