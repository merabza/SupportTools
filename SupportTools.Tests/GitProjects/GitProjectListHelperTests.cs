using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGitWork.Helpers;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.GitProjects;

//the project list of "Sync ... Gits V2": by group or name, and without remote projects unless they are included (D3)
public sealed class GitProjectListHelperTests : IDisposable
{
    private readonly Dictionary<string, ProjectModel> _projects;
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), $"GitProjectListHelperTests_{Guid.NewGuid():N}");

    public GitProjectListHelperTests()
    {
        Directory.CreateDirectory(_tempFolder);
        string missingFolder = Path.Combine(_tempFolder, "Missing");
        _projects = new Dictionary<string, ProjectModel>
        {
            ["LocalA"] = new() { ProjectGroupName = "G1", ProjectFolderName = _tempFolder },
            ["RemoteA"] = new() { ProjectGroupName = "G1", ProjectFolderName = missingFolder },
            ["LocalB"] = new() { ProjectGroupName = "G2", ProjectFolderName = _tempFolder },
            ["RemoteB"] = new() { ProjectGroupName = "G2", ProjectFolderName = missingFolder }
        };
    }

    public void Dispose()
    {
        Directory.Delete(_tempFolder, true);
    }

    [Theory]
    [InlineData(false, new[] { "LocalA", "LocalB" })]
    [InlineData(true, new[] { "LocalA", "RemoteA", "LocalB", "RemoteB" })]
    public void CreateProjectsList_WhenAllProjects_FiltersRemoteProjectsUnlessIncluded(bool includeRemoteProjects,
        string[] expected)
    {
        // Act
        List<string> result =
        [
            .. GitProjectListHelper.CreateProjectsList(_projects, null, null, includeRemoteProjects)
                .Select(x => x.Key)
        ];

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, new[] { "LocalB" })]
    [InlineData(true, new[] { "LocalB", "RemoteB" })]
    public void CreateProjectsList_WhenGroup_FiltersRemoteProjectsOfTheGroupUnlessIncluded(
        bool includeRemoteProjects, string[] expected)
    {
        // Act
        List<string> result =
        [
            .. GitProjectListHelper.CreateProjectsList(_projects, "G2", null, includeRemoteProjects)
                .Select(x => x.Key)
        ];

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CreateProjectsList_WhenRemoteProjectIsNamedAndIncluded_ReturnsIt()
    {
        // Act
        List<string> result =
            [.. GitProjectListHelper.CreateProjectsList(_projects, null, "RemoteA", true).Select(x => x.Key)];

        // Assert
        Assert.Equal(["RemoteA"], result);
    }

    [Fact]
    public void CreateProjectsList_WhenRemoteProjectIsNamedButNotIncluded_ReturnsNothing()
    {
        // Act
        List<string> result =
            [.. GitProjectListHelper.CreateProjectsList(_projects, null, "RemoteA", false).Select(x => x.Key)];

        // Assert
        Assert.Empty(result);
    }

    //the group wins over the name when both are given
    [Fact]
    public void CreateProjectsList_WhenGroupAndNameAreGiven_SelectsByGroup()
    {
        // Act
        List<string> result =
            [.. GitProjectListHelper.CreateProjectsList(_projects, "G1", "LocalB", true).Select(x => x.Key)];

        // Assert
        Assert.Equal(["LocalA", "RemoteA"], result);
    }
}
