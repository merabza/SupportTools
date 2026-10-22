using System;
using System.Collections.Generic;
using System.IO;
using LibGitData;
using LibGitData.Domain;
using LibGitData.Models;
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

    [Fact]
    public void Constructor_WhenCreated_HasVersionOneAndEmptyCollections()
    {
        // Act
        var sut = new ProjectModel();

        // Assert
        Assert.Equal(1, sut.MajorVersion);
        Assert.Equal(0, sut.MinorVersion);
        Assert.Empty(sut.RedundantFileNames);
        Assert.Empty(sut.FrontNpmPackageNames);
        Assert.Empty(sut.Endpoints);
        Assert.Empty(sut.RouteClasses);
        Assert.Empty(sut.GitProjectNames);
        Assert.Empty(sut.ScaffoldSeederGitProjectNames);
        Assert.Empty(sut.ServerInfos);
        Assert.Empty(sut.AllowToolsList);
    }

    [Fact]
    public void GetServerInfoRequired_WhenServerInfoExists_ReturnsIt()
    {
        // Arrange
        var serverInfo = new ServerInfoModel();
        var sut = new ProjectModel();
        sut.ServerInfos["PAZISI"] = serverInfo;

        // Act
        ServerInfoModel result = sut.GetServerInfoRequired("PAZISI");

        // Assert
        Assert.Same(serverInfo, result);
    }

    [Fact]
    public void GetServerInfoRequired_WhenServerInfoDoesNotExist_Throws()
    {
        // Arrange
        var sut = new ProjectModel();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetServerInfoRequired("dl360"));

        // Assert
        Assert.Equal("Server with name dl360 is not exists in project", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void MainProjectFileName_WhenMainProjectNameIsNotSpecified_ReturnsNull(string? mainProjectName)
    {
        // Arrange
        var sut = new ProjectModel { ProjectFolderName = @"D:\1WorkDotnet\App", MainProjectName = mainProjectName };

        // Act
        string? result = sut.MainProjectFileName(CreateGitProjects());

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void MainProjectFileName_WhenProjectFolderNameIsNotSpecified_ReturnsNull(string? projectFolderName)
    {
        // Arrange
        var sut = new ProjectModel { ProjectFolderName = projectFolderName, MainProjectName = "AppApi" };

        // Act
        string? result = sut.MainProjectFileName(CreateGitProjects());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void MainProjectFileName_WhenGitProjectExists_CombinesFolderRelativePathAndFileName()
    {
        // Arrange
        var sut = new ProjectModel { ProjectFolderName = @"D:\1WorkDotnet\App", MainProjectName = "AppApi" };

        // Act
        string? result = sut.MainProjectFileName(CreateGitProjects());

        // Assert
        Assert.Equal(Path.Combine(@"D:\1WorkDotnet\App", @"App\AppApi", "AppApi.csproj"), result);
    }

    //GitProjects.Create leaves out a record without a path or a file name, but the record of an existing one may change
    [Theory]
    [InlineData("", "AppApi.csproj")]
    [InlineData(@"App\AppApi", "")]
    public void MainProjectFileName_WhenRecordHasNoPathOrFileName_ReturnsNull(string projectRelativePath,
        string projectFileName)
    {
        // Arrange
        LibGitData.Models.GitProjects gitProjects = CreateGitProjects();
        GitProjectDataDomain gitProject = gitProjects.GetGitProjectByKey("AppApi");
        gitProject.ProjectRelativePath = projectRelativePath;
        gitProject.ProjectFileName = projectFileName;
        var sut = new ProjectModel { ProjectFolderName = @"D:\1WorkDotnet\App", MainProjectName = "AppApi" };

        // Act
        string? result = sut.MainProjectFileName(gitProjects);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void SpaProjectFolderRelativePath_WhenSpaProjectNameIsNotSpecified_ReturnsNull(string? spaProjectName)
    {
        // Arrange
        var sut = new ProjectModel { SpaProjectName = spaProjectName };

        // Act
        string? result = sut.SpaProjectFolderRelativePath(CreateGitProjects());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SpaProjectFolderRelativePath_WhenGitProjectExists_ReturnsItsRelativePath()
    {
        // Arrange
        var sut = new ProjectModel { SpaProjectName = "AppFront" };

        // Act
        string? result = sut.SpaProjectFolderRelativePath(CreateGitProjects());

        // Assert
        Assert.Equal(@"App\AppFront", result);
    }

    [Fact]
    public void SpaProjectFolderRelativePath_WhenRecordHasNoPath_ReturnsNull()
    {
        // Arrange
        LibGitData.Models.GitProjects gitProjects = CreateGitProjects();
        gitProjects.GetGitProjectByKey("AppFront").ProjectRelativePath = " ";
        var sut = new ProjectModel { SpaProjectName = "AppFront" };

        // Act
        string? result = sut.SpaProjectFolderRelativePath(gitProjects);

        // Assert
        Assert.Null(result);
    }

    //the callers that must not stop check IsSpaProjectMissing first (D3)
    [Fact]
    public void SpaProjectFolderRelativePath_WhenGitProjectIsMissing_Throws()
    {
        // Arrange
        var sut = new ProjectModel { SpaProjectName = "Missing" };

        // Act
        var exception = Assert.Throws<Exception>(() => sut.SpaProjectFolderRelativePath(CreateGitProjects()));

        // Assert
        Assert.Equal("GitProject With Key Missing does not found", exception.Message);
    }

    [Theory]
    [InlineData("Missing", true)]
    [InlineData("AppFront", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    public void IsSpaProjectMissing_WhenCalled_TellsWhetherTheSpaRecordIsMissing(string? spaProjectName, bool expected)
    {
        // Arrange
        var sut = new ProjectModel { SpaProjectName = spaProjectName };

        // Act
        bool result = sut.IsSpaProjectMissing(CreateGitProjects());

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetGitProjectNamesByGitCollectionType_WhenMain_ReturnsProjectGits()
    {
        // Arrange
        var sut = new ProjectModel { GitProjectNames = ["App"], ScaffoldSeederGitProjectNames = ["AppSeeder"] };

        // Act
        List<string> result = sut.GetGitProjectNamesByGitCollectionType(EGitCol.Main);

        // Assert
        Assert.Same(sut.GitProjectNames, result);
    }

    [Fact]
    public void GetGitProjectNamesByGitCollectionType_WhenScaffoldSeed_ReturnsSeederGits()
    {
        // Arrange
        var sut = new ProjectModel { GitProjectNames = ["App"], ScaffoldSeederGitProjectNames = ["AppSeeder"] };

        // Act
        List<string> result = sut.GetGitProjectNamesByGitCollectionType(EGitCol.ScaffoldSeed);

        // Assert
        Assert.Same(sut.ScaffoldSeederGitProjectNames, result);
    }

    [Fact]
    public void GetGitProjectNamesByGitCollectionType_WhenCollectionIsUnknown_ReturnsEmptyList()
    {
        // Arrange
        var sut = new ProjectModel { GitProjectNames = ["App"], ScaffoldSeederGitProjectNames = ["AppSeeder"] };

        // Act
        List<string> result = sut.GetGitProjectNamesByGitCollectionType((EGitCol)99);

        // Assert
        Assert.Empty(result);
    }

    private static LibGitData.Models.GitProjects CreateGitProjects()
    {
        return LibGitData.Models.GitProjects.Create(null,
            new Dictionary<string, GitProjectDataModel>
            {
                ["AppApi"] =
                    new()
                    {
                        GitName = "App", ProjectRelativePath = @"App\AppApi", ProjectFileName = "AppApi.csproj"
                    },
                ["AppFront"] = new()
                {
                    GitName = "App", ProjectRelativePath = @"App\AppFront", ProjectFileName = "AppFront.esproj"
                }
            });
    }
}
