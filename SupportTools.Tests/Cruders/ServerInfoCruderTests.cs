using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.Tests.Registry.Mappers;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

public sealed class ServerInfoCruderTests
{
    private readonly SupportToolsParameters _parameters = new()
    {
        Projects = { [ProjectTestData.Name] = ProjectTestData.NewProject() }
    };

    private readonly Mock<IParametersManager> _parametersManager = new();

    public ServerInfoCruderTests()
    {
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
    }

    //the copy is a command of the whole record, like the delete and the edit before it
    [Fact]
    public void GetItemMenu_WhenServerExists_OffersCopyRightAfterEditAllFieldsInSequence()
    {
        // Arrange
        string[] expected = ["Delete this record", "Edit All fields in sequence", "Copy Server..."];
        var sut = ServerInfoCruder.Create(new Mock<IApplication>().Object, new Mock<IServiceProvider>().Object,
            new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object, _parametersManager.Object,
            ProjectTestData.Name);

        // Act
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(sut.GetItemMenu(ProjectTestData.ProdKey));

        // Assert
        Assert.Equal(expected, menuItems.Take(3).Select(x => x.MenuItemName));
        Assert.IsType<CopyServerInfoCliMenuCommand>(menuItems[2].CliMenuCommand);
    }
    [Theory]
    [InlineData(EProjectType.IsService, true, new[]
    {
        "ServerName", "EnvironmentName", "WebAgentNameForCheck", "ServerSidePort", "ApiVersionId", "ServiceUserName",
        "AppSettingsJsonSourceFileName", "AppSettingsEncodedJsonFileName", "OpenPort", "CurrentDatabaseParameters",
        "NewDatabaseParameters"
    })]
    [InlineData(EProjectType.Standard, true, new[]
    {
        "ServerName", "EnvironmentName", "AppSettingsJsonSourceFileName", "AppSettingsEncodedJsonFileName",
        "CurrentDatabaseParameters", "NewDatabaseParameters"
    })]
    [InlineData(EProjectType.IsService, false, new[]
    {
        "ServerName", "EnvironmentName", "WebAgentNameForCheck", "ServerSidePort", "ApiVersionId", "ServiceUserName",
        "AppSettingsJsonSourceFileName", "AppSettingsEncodedJsonFileName", "OpenPort"
    })]
    [InlineData(EProjectType.IsDbTools, false, new[]
    {
        "ServerName", "EnvironmentName", "CurrentDatabaseParameters", "NewDatabaseParameters"
    })]
    public void Create_WhenCalled_RegistersFieldEditorsByProjectTypeAndUsesDatabase(EProjectType projectType,
        bool usesDatabase, string[] expected)
    {
        // Arrange
        _parameters.Projects[ProjectTestData.Name] = new ProjectModel
        {
            ProjectType = projectType, UsesDatabase = usesDatabase
        };

        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal(expected, fieldEditors.Select(x => x.PropertyName));
    }

    [Fact]
    public void Create_WhenProjectIsMissing_Throws()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => ServerInfoCruder.Create(new Mock<IApplication>().Object,
            new Mock<IServiceProvider>().Object, new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object,
            _parametersManager.Object, "NoSuchProject"));
    }

    [Theory]
    [InlineData(EProjectType.IsService, true, new[] { "Dev Base To Server Copier", "Program Updater" })]
    [InlineData(EProjectType.IsService, false, new[] { "Program Updater" })]
    [InlineData(EProjectType.IsDbTools, true, new[] { "Dev Base To Server Copier" })]
    public void FillDetailsSubMenu_WhenServerHasAllowedTools_ListsVisibleOnes(EProjectType projectType,
        bool usesDatabase, string[] expected)
    {
        // Arrange
        var project = new ProjectModel { ProjectType = projectType, UsesDatabase = usesDatabase };
        project.ServerInfos[ProjectTestData.ProdKey] = new ServerInfoModel
        {
            ServerName = "PAZISI",
            EnvironmentName = "Production",
            AllowToolsList = [EProjectServerTools.ProgramUpdater, EProjectServerTools.DevBaseToServerCopier]
        };
        _parameters.Projects[ProjectTestData.Name] = project;
        var menuSet = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(menuSet, ProjectTestData.ProdKey);

        // Assert
        Assert.Equal(expected,
            CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand)
                .OfType<ProjectServerToolTaskCliMenuCommand>().Select(x => x.Name));
        Assert.Single(CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand)
            .OfType<SelectServerAllowToolsCliMenuCommand>());
    }

    [Fact]
    public void FillDetailsSubMenu_WhenServerIsMissing_AddsNoTools()
    {
        // Arrange
        var menuSet = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(menuSet, "NoSuchServer");

        // Assert
        Assert.Empty(CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand)
            .OfType<ProjectServerToolTaskCliMenuCommand>());
    }

    [Fact]
    public void GetListMenu_WhenCalled_NamesRecordsAsServers()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal("Servers", listMenu.Caption);
        Assert.Equal("New Server", CliMenuTestAccess.GetMenuItems(listMenu)[0].MenuItemName);
    }

    //the key of a server comes from its fields, so the details have no "Record Name" but have every field
    [Fact]
    public void FillDetailsSubMenu_WhenServerExists_ListsFieldsWithoutRecordName()
    {
        // Arrange
        var menuSet = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(menuSet, ProjectTestData.ProdKey);

        // Assert
        List<string> names = [.. CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand.Name)];
        Assert.DoesNotContain("Record Name", names);
        Assert.Contains("Server Name", names);
        Assert.Contains("Environment Name", names);
    }

    [Fact]
    public void FillDetailsSubMenu_WhenServerHasNoAllowToolsList_AddsNoTools()
    {
        // Arrange
        _parameters.Projects[ProjectTestData.Name].ServerInfos[ProjectTestData.ProdKey].AllowToolsList = null;
        var menuSet = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(menuSet, ProjectTestData.ProdKey);

        // Assert
        Assert.Empty(CliMenuTestAccess.GetMenuItems(menuSet).Select(x => x.CliMenuCommand)
            .OfType<ProjectServerToolTaskCliMenuCommand>());
    }

    private ServerInfoCruder CreateSut()
    {
        return ServerInfoCruder.Create(new Mock<IApplication>().Object, new Mock<IServiceProvider>().Object,
            new Mock<ILogger>().Object, new Mock<IHttpClientFactory>().Object, _parametersManager.Object,
            ProjectTestData.Name);
    }
}
