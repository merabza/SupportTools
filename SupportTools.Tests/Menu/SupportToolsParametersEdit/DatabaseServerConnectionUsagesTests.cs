using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParametersDataEdit.Models;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibDatabaseParameters;
using SupportTools.DependencyInjection;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.SupportToolsParametersEdit;

public sealed class DatabaseServerConnectionUsagesTests
{
    private const string ConnectionName = "Conn";

    private readonly SupportToolsParameters _parameters = new()
    {
        DatabaseServerConnections =
        {
            [ConnectionName] = new DatabaseServerConnectionData
            {
                DatabaseServerProvider = EDatabaseProvider.SqlServer, ServerAddress = "TestSqlServer"
            }
        }
    };

    //the order of the menus: Project Creator comes before the groups, the groups and their projects are in name order,
    //the database parameters of a project come before those of its servers, the servers are in name order
    [Fact]
    public void Find_ListsEveryPlaceOfTheConnectionInMenuOrder()
    {
        // Arrange
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            DeveloperDbConnectionName = ConnectionName
        };
        _parameters.Projects["Beta"] = new ProjectModel
        {
            ProjectGroupName = "G2", DevDatabaseParameters = UsesConnection(), ProdCopyDatabaseParameters = Uses("Other")
        };
        _parameters.Projects["Alpha"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProdCopyDatabaseParameters = UsesConnection(),
            ServerInfos =
            {
                ["k2"] = new ServerInfoModel
                {
                    ServerName = "Srv",
                    EnvironmentName = "Test",
                    CurrentDatabaseParameters = UsesConnection(),
                    NewDatabaseParameters = UsesConnection()
                },
                ["k1"] = new ServerInfoModel
                {
                    ServerName = "Srv", EnvironmentName = "Dev", NewDatabaseParameters = UsesConnection()
                }
            }
        };
        //the tools look a connection up by its exact name
        _parameters.Projects["Gamma"] = new ProjectModel { ProjectGroupName = "G1", DevDatabaseParameters = Uses("conn") };

        // Act
        List<DatabaseServerConnectionUsage> result = DatabaseServerConnectionUsages.Find(_parameters, ConnectionName);

        // Assert
        List<string> expectedPlaces =
        [
            "Project Creator", "G1/Alpha/Prod Copy Database Parameters", "G1/Alpha/Srv|Dev/New Database Parameters",
            "G1/Alpha/Srv|Test/Current Database Parameters", "G1/Alpha/Srv|Test/New Database Parameters",
            "G2/Beta/Dev Database Parameters"
        ];
        Assert.Equal(expectedPlaces, result.Select(x => x.Name));
        Assert.Equal(expectedPlaces, result.Select(x => x.MenuLink));
    }

    [Fact]
    public void Find_WhenNothingUsesTheConnection_ReturnsNothing()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            DevDatabaseParameters = new DatabaseParameters(),
            ServerInfos = { ["k"] = new ServerInfoModel { ServerName = "Srv", EnvironmentName = "Prod" } }
        };

        // Act & Assert
        Assert.Empty(DatabaseServerConnectionUsages.Find(_parameters, ConnectionName));
    }

    //UsesDatabase off hides the database parameters in the project menu and in its server menus
    [Fact]
    public void Find_WhenTheProjectDoesNotUseTheDatabase_MarksItsPlacesAndLinksToTheProjectMenu()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            UsesDatabase = false,
            DevDatabaseParameters = UsesConnection(),
            ServerInfos =
            {
                ["k"] = new ServerInfoModel
                {
                    ServerName = "Srv", EnvironmentName = "Prod", CurrentDatabaseParameters = UsesConnection()
                }
            }
        };

        // Act
        List<DatabaseServerConnectionUsage> result = DatabaseServerConnectionUsages.Find(_parameters, ConnectionName);

        // Assert
        Assert.Equal(
        [
            new DatabaseServerConnectionUsage("G1/App/Dev Database Parameters (Uses Database is off)", "G1/App"),
            new DatabaseServerConnectionUsage("G1/App/Srv|Prod/Current Database Parameters (Uses Database is off)",
                "G1/App")
        ], result);
    }

    //a project of database tools shows its database parameters whatever UsesDatabase says
    [Fact]
    public void Find_WhenADbToolsProjectHasUsesDatabaseOff_ListsItsPlacesUnmarked()
    {
        // Arrange
        _parameters.Projects["AppDbTools"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectType = EProjectType.IsDbTools,
            UsesDatabase = false,
            DevDatabaseParameters = UsesConnection()
        };

        // Act
        DatabaseServerConnectionUsage result =
            Assert.Single(DatabaseServerConnectionUsages.Find(_parameters, ConnectionName));

        // Assert
        Assert.Equal(
            new DatabaseServerConnectionUsage("G1/AppDbTools/Dev Database Parameters",
                "G1/AppDbTools/Dev Database Parameters"), result);
    }

    //HideRemoteOnlyProjects leaves a remote project out of the menus; that mark wins over UsesDatabase
    [Fact]
    public void Find_WhenHideRemoteOnlyProjectsHidesTheProject_MarksItsPlacesAndLinksToTheParametersEditor()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;
        string missingFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _parameters.Projects["Local"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectFolderName = AppContext.BaseDirectory,
            DevDatabaseParameters = UsesConnection()
        };
        _parameters.Projects["Remote"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectFolderName = missingFolder,
            UsesDatabase = false,
            DevDatabaseParameters = UsesConnection()
        };

        // Act
        List<DatabaseServerConnectionUsage> result = DatabaseServerConnectionUsages.Find(_parameters, ConnectionName);

        // Assert
        Assert.Equal(
        [
            new DatabaseServerConnectionUsage("G1/Local/Dev Database Parameters",
                "G1/Local/Dev Database Parameters"),
            new DatabaseServerConnectionUsage("G1/Remote/Dev Database Parameters (Hide Remote Only Projects is on)",
                SupportToolsParametersEditor.MenuName)
        ], result);
    }

    //a project without a group is in the group menu that the projects list names for it
    [Fact]
    public void Find_WhenTheProjectHasNoGroup_LinksThroughTheNoGroupMenu()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel { DevDatabaseParameters = UsesConnection() };

        // Act
        DatabaseServerConnectionUsage result =
            Assert.Single(DatabaseServerConnectionUsages.Find(_parameters, ConnectionName));

        // Assert
        Assert.Equal($"{SupportToolsParameters.FixProjectGroupName(null)}/App/Dev Database Parameters",
            result.MenuLink);
    }

    //the way CliAppLoop.GoToMenu follows a link through the real menus; every link ends in a menu that shows the
    //connection, or, for a hidden place, the parameter that hides it. The group BaGetter has the name of a main menu
    //command, as in the real parameters
    [Fact]
    public async Task Find_EveryMenuLinkLeadsToTheMenuThatShowsThePlace()
    {
        // Arrange
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            DeveloperDbConnectionName = ConnectionName
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectGroupName = "BaGetter",
            DevDatabaseParameters = UsesConnection(),
            ProdCopyDatabaseParameters = UsesConnection(),
            ServerInfos =
            {
                ["k"] = new ServerInfoModel
                {
                    ServerName = "Srv",
                    EnvironmentName = "Prod",
                    CurrentDatabaseParameters = UsesConnection(),
                    NewDatabaseParameters = UsesConnection()
                }
            }
        };
        _parameters.Projects["Old"] = new ProjectModel
        {
            ProjectGroupName = "G1", UsesDatabase = false, DevDatabaseParameters = UsesConnection()
        };
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        List<DatabaseServerConnectionUsage> usages = DatabaseServerConnectionUsages.Find(_parameters, ConnectionName);

        // Assert
        Assert.Equal(6, usages.Count);
        Assert.Equal("Conn (SqlServer: TestSqlServer)",
            await GetMenuItemStatus(serviceProvider, usages[0].MenuLink, "Developer Db Connection Name"));
        foreach (DatabaseServerConnectionUsage usage in usages.Skip(1).Take(4))
        {
            Assert.Equal("Conn (SqlServer: TestSqlServer)",
                await GetMenuItemStatus(serviceProvider, usage.MenuLink, "Db Connection Name"));
        }

        Assert.Equal(bool.FalseString, await GetMenuItemStatus(serviceProvider, usages[5].MenuLink, "Uses Database"));
    }

    [Fact]
    public async Task Find_HiddenRemoteProjectLinkLeadsToTheHideRemoteOnlyProjectsParameter()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;
        _parameters.Projects["Remote"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectFolderName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            DevDatabaseParameters = UsesConnection()
        };
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        DatabaseServerConnectionUsage usage =
            Assert.Single(DatabaseServerConnectionUsages.Find(_parameters, ConnectionName));

        // Assert
        Assert.Equal(bool.TrueString,
            await GetMenuItemStatus(serviceProvider, usage.MenuLink, "Hide Remote Only Projects"));
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddServices("SupportToolsTests", _parameters, @"D:\Fake\SupportTools.json");
        return services.BuildServiceProvider();
    }

    private static async Task<string?> GetMenuItemStatus(IServiceProvider serviceProvider, string menuLink,
        string menuItemName)
    {
        CliMenuSet? menu = await new SupportToolsMenuBuilder(serviceProvider).BuildMainMenu();
        foreach (string linkItemName in menuLink.Split('/'))
        {
            CliMenuItem? linkItem = menu?.GetLastMenuItemWithName(linkItemName);
            Assert.True(linkItem is not null, $"{linkItemName} of the menu link {menuLink} not found");
            menu = linkItem.CliMenuCommand.GetSubMenu();
        }

        CliMenuItem? menuItem = menu?.GetMenuItemWithName(menuItemName);
        Assert.True(menuItem is not null, $"{menuItemName} not found in the menu of {menuLink}");
        menuItem.CliMenuCommand.CountStatus();
        return menuItem.CliMenuCommand.StatusString;
    }

    private static DatabaseParameters UsesConnection()
    {
        return Uses(ConnectionName);
    }

    private static DatabaseParameters Uses(string connectionName)
    {
        return new DatabaseParameters { DbConnectionName = connectionName };
    }
}
