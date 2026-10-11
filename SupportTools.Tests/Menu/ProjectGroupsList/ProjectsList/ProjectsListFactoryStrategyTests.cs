using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList;

//G1: LocalB, RemoteA and LocalA (in that order); G2: LocalC
public sealed class ProjectsListFactoryStrategyTests : IDisposable
{
    private readonly SupportToolsMenuParameters _menuParameters = new() { ProjectGroupName = "G1" };
    private readonly SupportToolsParameters _parameters = new();
    private readonly ServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ProjectsListFactoryStrategyTests()
    {
        AddProject("LocalB", "G1", true);
        AddProject("RemoteA", "G1", false);
        AddProject("LocalA", "G1", true);
        AddProject("LocalC", "G2", true);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void CreateMenuCommandsList_WhenCalled_ListsTheProjectsOfTheCurrentGroupInOrder()
    {
        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["LocalA", "LocalB", "RemoteA"], result.Select(x => x.Name));
        Assert.All(result, x => Assert.IsType<ProjectSubMenuCliMenuCommand>(x));
    }

    [Fact]
    public void CreateMenuCommandsList_WhenRemoteOnlyProjectsAreHidden_ListsOnlyLocalProjects()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;

        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["LocalA", "LocalB"], result.Select(x => x.Name));
    }

    private ProjectsListFactoryStrategy CreateSut()
    {
        return new ProjectsListFactoryStrategy(_serviceProvider, _menuParameters,
            new ParametersManager(null, _parameters));
    }

    private void AddProject(string projectName, string groupName, bool isLocal)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing")
        };
    }
}
