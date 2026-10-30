using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.Menu;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectToolsList;
using SupportToolsData;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.ProjectGroupsList.ProjectsList.ProjectToolsList;

public sealed class ProjectToolsListFactoryStrategyTests
{
    private const string ProjectName = "MyProject";

    private readonly Mock<ILogger<ProjectToolsListFactoryStrategy>> _logger = new();
    private readonly SupportToolsMenuParameters _menuParameters = new() { ProjectName = ProjectName };
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();

    public ProjectToolsListFactoryStrategyTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    [Fact]
    public void CreateMenuCommandsList_WhenProjectIsMissing_ReturnsEmptyListAndLogsWarning()
    {
        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Empty(result);
#pragma warning disable CA1873
        _logger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString() == $"Project not found: {ProjectName}"), null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
#pragma warning restore CA1873
    }

    [Fact]
    public void CreateMenuCommandsList_WhenProjectHasAllowedTools_ListsDefinedOnesOrderedByName()
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            AllowToolsList =
            [
                EProjectTools.GenerateApiRoutes, EProjectTools.DropDevDatabase, EProjectTools.AnaliseDevDatabase,
                (EProjectTools)99
            ]
        };

        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["Analise Dev Database", "Drop Dev Database", "Generate Api Routes"], result.Select(x => x.Name));
        Assert.All(result, x => Assert.IsType<ProjectToolTaskCliMenuCommand>(x));
    }

    [Fact]
    public void CreateMenuCommandsList_WhenProjectDoesNotUseDatabase_HidesAllowedDatabaseTools()
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            UsesDatabase = false, AllowToolsList = [EProjectTools.RecreateDevDatabase, EProjectTools.GenerateApiRoutes]
        };

        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["Generate Api Routes"], result.Select(x => x.Name));
    }

    [Fact]
    public void CreateMenuCommandsList_WhenProjectIsDbTools_HidesAllowedNonDatabaseTools()
    {
        // Arrange
        _parameters.Projects[ProjectName] = new ProjectModel
        {
            ProjectType = EProjectType.IsDbTools,
            AllowToolsList = [EProjectTools.JetBrainsCleanupCode, EProjectTools.DropDevDatabase]
        };

        // Act
        List<CliMenuCommand> result = CreateSut().CreateMenuCommandsList();

        // Assert
        Assert.Equal(["Drop Dev Database"], result.Select(x => x.Name));
    }

    private ProjectToolsListFactoryStrategy CreateSut()
    {
        return new ProjectToolsListFactoryStrategy(_logger.Object, _menuParameters, _parametersManager.Object,
            new Mock<IServiceProvider>().Object);
    }
}
