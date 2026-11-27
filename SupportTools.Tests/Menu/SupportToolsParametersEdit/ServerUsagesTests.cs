using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.DependencyInjection;
using SupportTools.DependencyInjection;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportTools.Models;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Menu.SupportToolsParametersEdit;

public sealed class ServerUsagesTests
{
    private const string ServerName = "Srv";

    private readonly SupportToolsParameters _parameters = new()
    {
        Servers = { [ServerName] = new ServerDataModel(), ["Other"] = new ServerDataModel() }
    };

    //the order of the menus: Support Tools Parameters Editor and Project Creator come before the groups, the groups and
    //their projects are in name order, the servers of a project are in name order
    [Fact]
    public void Find_ListsEveryPlaceOfTheServerInMenuOrder()
    {
        // Arrange
        _parameters.CurrentMachineServerName = ServerName;
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            ProductionServerName = ServerName
        };
        _parameters.Projects["Beta"] = new ProjectModel
        {
            ProjectGroupName = "G2", ServerInfos = { ["k"] = UsesServer("Prod") }
        };
        _parameters.Projects["Alpha"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ServerInfos =
            {
                ["k3"] = UsesServer("Test"),
                ["k2"] = new ServerInfoModel { ServerName = "Other", EnvironmentName = "Prod" },
                ["k1"] = UsesServer("Dev")
            }
        };

        // Act
        List<ServerUsage> result = ServerUsages.Find(_parameters, ServerName);

        // Assert
        List<string> expectedPlaces =
        [
            "Support Tools Parameters Editor", "Project Creator", "G1/Alpha/Srv|Dev", "G1/Alpha/Srv|Test",
            "G2/Beta/Srv|Prod"
        ];
        Assert.Equal(expectedPlaces, result.Select(x => x.Name));
        Assert.Equal(expectedPlaces, result.Select(x => x.MenuLink));
    }

    //ServersIsLocalCalculator compares the server of this computer ignoring case; the project creator and the tools of
    //a project server look a server up by its exact name
    [Fact]
    public void Find_WhenTheNamesDifferOnlyByCase_CountsOnlyTheCurrentMachineServer()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "SRV";
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            ProductionServerName = "srv"
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ServerInfos = { ["k"] = new ServerInfoModel { ServerName = "srv", EnvironmentName = "Prod" } }
        };

        // Act
        ServerUsage result = Assert.Single(ServerUsages.Find(_parameters, ServerName));

        // Assert
        Assert.Equal(new ServerUsage(SupportToolsParametersEditor.MenuName, SupportToolsParametersEditor.MenuName),
            result);
    }

    [Fact]
    public void Find_WhenNothingUsesTheServer_ReturnsNothing()
    {
        // Arrange
        _parameters.CurrentMachineServerName = "Other";
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            ProductionServerName = "Other"
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ServerInfos = { ["k"] = new ServerInfoModel { ServerName = "Other", EnvironmentName = "Prod" } }
        };

        // Act & Assert
        Assert.Empty(ServerUsages.Find(_parameters, ServerName));
    }

    //HideRemoteOnlyProjects leaves a remote project out of the menus
    [Fact]
    public void Find_WhenHideRemoteOnlyProjectsHidesTheProject_MarksItsPlacesAndLinksToTheParametersEditor()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;
        _parameters.Projects["Local"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectFolderName = AppContext.BaseDirectory,
            ServerInfos = { ["k"] = UsesServer("Prod") }
        };
        _parameters.Projects["Remote"] = new ProjectModel
        {
            ProjectGroupName = "G1",
            ProjectFolderName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            ServerInfos = { ["k"] = UsesServer("Prod") }
        };

        // Act
        List<ServerUsage> result = ServerUsages.Find(_parameters, ServerName);

        // Assert
        Assert.Equal(
        [
            new ServerUsage("G1/Local/Srv|Prod", "G1/Local/Srv|Prod"),
            new ServerUsage("G1/Remote/Srv|Prod (Hide Remote Only Projects is on)",
                SupportToolsParametersEditor.MenuName)
        ], result);
    }

    //a project without a group is in the group menu that the projects list names for it
    [Fact]
    public void Find_WhenTheProjectHasNoGroup_LinksThroughTheNoGroupMenu()
    {
        // Arrange
        _parameters.Projects["App"] = new ProjectModel { ServerInfos = { ["k"] = UsesServer("Prod") } };

        // Act
        ServerUsage result = Assert.Single(ServerUsages.Find(_parameters, ServerName));

        // Assert
        Assert.Equal($"{SupportToolsParameters.FixProjectGroupName(null)}/App/Srv|Prod", result.MenuLink);
    }

    //the way CliAppLoop.GoToMenu follows a link through the real menus; every link ends in a menu that shows the
    //server. The group BaGetter has the name of a main menu command, as in the real parameters
    [Fact]
    public async Task Find_EveryMenuLinkLeadsToTheMenuThatShowsTheServer()
    {
        // Arrange
        _parameters.CurrentMachineServerName = ServerName;
        _parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters
        {
            ProductionServerName = ServerName
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectGroupName = "BaGetter", ServerInfos = { ["k"] = UsesServer("Prod") }
        };
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        List<ServerUsage> usages = ServerUsages.Find(_parameters, ServerName);

        // Assert
        Assert.Equal(3, usages.Count);
        Assert.Equal(ServerName,
            await GetMenuItemStatus(serviceProvider, usages[0].MenuLink, "Current Machine Server Name"));
        Assert.Equal(ServerName,
            await GetMenuItemStatus(serviceProvider, usages[1].MenuLink, "Production Server Name"));
        Assert.Equal(ServerName, await GetMenuItemStatus(serviceProvider, usages[2].MenuLink, "Server Name"));
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
            ServerInfos = { ["k"] = UsesServer("Prod") }
        };
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        ServerUsage usage = Assert.Single(ServerUsages.Find(_parameters, ServerName));

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

    private static ServerInfoModel UsesServer(string environmentName)
    {
        return new ServerInfoModel { ServerName = ServerName, EnvironmentName = environmentName };
    }
}
