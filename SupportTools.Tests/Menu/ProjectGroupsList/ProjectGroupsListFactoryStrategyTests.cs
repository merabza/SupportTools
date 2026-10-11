using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList;

//G1: LocalA and RemoteA; G2: RemoteB only; G3: LocalC and LocalD; a project without a group
public sealed class ProjectGroupsListFactoryStrategyTests : IDisposable
{
    private readonly SupportToolsMenuParameters _menuParameters = new();
    private readonly SupportToolsParameters _parameters = new();
    private readonly ServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;

    public ProjectGroupsListFactoryStrategyTests()
    {
        AddProject("LocalD", "G3", true);
        AddProject("RemoteB", "G2", false);
        AddProject("LocalA", "G1", true);
        AddProject("RemoteA", "G1", false);
        AddProject("LocalC", "G3", true);
        AddProject("NoGroup", null, true);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        Directory.Delete(_tempFolder, true);
    }

    [Fact]
    public void CreateMenuCommandsList_WhenCalled_ListsEveryGroupOnceInOrder()
    {
        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["__No Group__", "G1", "G2", "G3"], result.Select(x => x.Name));
        Assert.All(result, x => Assert.IsType<ProjectGroupSubMenuCliMenuCommand>(x));
    }

    [Fact]
    public void CreateMenuCommandsList_WhenRemoteOnlyProjectsAreHidden_HidesGroupsWithoutLocalProjects()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;

        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["__No Group__", "G1", "G3"], result.Select(x => x.Name));
    }

    private ProjectGroupsListFactoryStrategy CreateSut()
    {
        return new ProjectGroupsListFactoryStrategy(_serviceProvider, _menuParameters,
            new ParametersManager(null, _parameters));
    }

    private void AddProject(string projectName, string? groupName, bool isLocal)
    {
        _parameters.Projects[projectName] = new ProjectModel
        {
            ProjectGroupName = groupName,
            ProjectFolderName = isLocal ? _tempFolder : Path.Combine(_tempFolder, "Missing")
        };
    }
}
