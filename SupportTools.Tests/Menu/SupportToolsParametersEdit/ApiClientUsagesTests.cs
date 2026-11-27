using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParametersApiClientsEdit;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using SupportTools.DependencyInjection;
using SupportTools.Menu.SupportToolsParametersEdit;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Menu.SupportToolsParametersEdit;

public sealed class ApiClientUsagesTests
{
    private const string ApiClientName = "Client";
    private const string EditorLink = "Support Tools Parameters Editor";

    //the status of an api client field: the client name and its server
    private const string FieldStatus = "Client (http://127.0.0.1:0/)";

    private readonly SupportToolsParameters _parameters = new()
    {
        ApiClients =
        {
            [ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/" },
            ["Other"] = new ApiClientSettings { Server = "http://127.0.0.1:1/" }
        }
    };

    //the order of the menus: Support Tools Parameters Editor comes before the groups, its own fields before its
    //Database Server Connections and Servers lists; the records of a list, the groups, their projects and the servers
    //of a project are in name order. The name ends with the field, the link is the menu that holds the field
    [Fact]
    public void Find_ListsEveryPlaceOfTheClientInMenuOrder()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _parameters.LocalPackageManagerWebApiClientName = ApiClientName;
        _parameters.DatabaseServerConnections["Db2"] = WebAgentConnection(ApiClientName);
        _parameters.DatabaseServerConnections["Db1"] = WebAgentConnection(ApiClientName);
        _parameters.DatabaseServerConnections["Db0"] = WebAgentConnection("Other");
        _parameters.Servers["Srv2"] = new ServerDataModel { WebAgentInstallerName = ApiClientName };
        _parameters.Servers["Srv1"] = new ServerDataModel
        {
            WebAgentName = ApiClientName, WebAgentInstallerName = ApiClientName
        };
        _parameters.Servers["Srv0"] = new ServerDataModel { WebAgentName = "Other", WebAgentInstallerName = "Other" };
        _parameters.Projects["Beta"] = ServiceProject("G2", ["Prod"]);
        _parameters.Projects["Gamma"] = ServiceProject("G1", ["Prod"]);
        _parameters.Projects["Alpha"] = ServiceProject("G1", ["Test", "Dev"]);
        _parameters.Projects["Alpha"].ServerInfos["k0"] = new ServerInfoModel
        {
            ServerName = "Srv1", EnvironmentName = "Prod", WebAgentNameForCheck = "Other"
        };

        // Act
        List<ApiClientUsage> result = ApiClientUsages.Find(_parameters, ApiClientName);

        // Assert
        List<string> expectedPlaces =
        [
            $"{EditorLink}/Support Tools Server Web Api Client Name",
            $"{EditorLink}/Local Package Manager Web Api Client Name",
            $"{EditorLink}/Database Server Connections/Db1/Db Web Agent Name",
            $"{EditorLink}/Database Server Connections/Db2/Db Web Agent Name",
            $"{EditorLink}/Servers/Srv1/Web Agent Name",
            $"{EditorLink}/Servers/Srv1/Web Agent Installer Name",
            $"{EditorLink}/Servers/Srv2/Web Agent Installer Name",
            "G1/Alpha/Srv1|Dev/Web Agent Name For Check",
            "G1/Alpha/Srv1|Test/Web Agent Name For Check",
            "G1/Gamma/Srv1|Prod/Web Agent Name For Check",
            "G2/Beta/Srv1|Prod/Web Agent Name For Check"
        ];
        Assert.Equal(expectedPlaces, result.Select(x => x.Name));
        Assert.Equal(expectedPlaces.Select(x => x[..x.LastIndexOf('/')]), result.Select(x => x.MenuLink));
    }

    //the tools look a client up by its exact name
    [Theory]
    [InlineData("Other")]
    [InlineData("client")]
    [InlineData(null)]
    public void Find_WhenNothingUsesTheClient_ReturnsNothing(string? usedName)
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = usedName;
        _parameters.LocalPackageManagerWebApiClientName = usedName;
        _parameters.DatabaseServerConnections["Db"] = WebAgentConnection(usedName);
        _parameters.Servers["Srv"] = new ServerDataModel
        {
            WebAgentName = usedName, WebAgentInstallerName = usedName
        };
        _parameters.Projects["App"] = new ProjectModel
        {
            ProjectType = EProjectType.IsService,
            ServerInfos =
            {
                ["k"] = new ServerInfoModel
                {
                    ServerName = "Srv", EnvironmentName = "Prod", WebAgentNameForCheck = usedName
                }
            }
        };

        // Act & Assert
        Assert.Empty(ApiClientUsages.Find(_parameters, ApiClientName));
    }

    //the web agent of a connection shows only for the WebAgent provider; the provider is in the connection menu
    [Theory]
    [InlineData(EDatabaseProvider.None)]
    [InlineData(EDatabaseProvider.SqlServer)]
    [InlineData(EDatabaseProvider.MariaDb)]
    public void Find_WhenTheConnectionIsNotAWebAgentConnection_MarksItsPlaceAndLinksToTheConnection(
        EDatabaseProvider provider)
    {
        // Arrange
        _parameters.DatabaseServerConnections["Db"] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = provider, DbWebAgentName = ApiClientName
        };

        // Act
        ApiClientUsage result = Assert.Single(ApiClientUsages.Find(_parameters, ApiClientName));

        // Assert
        Assert.Equal(
            new ApiClientUsage(
                $"{EditorLink}/Database Server Connections/Db/Db Web Agent Name (Database Server Provider is {provider})",
                $"{EditorLink}/Database Server Connections/Db"), result);
    }

    //the web agent for check shows only in the server menus of a service; the project type is in the project menu
    [Theory]
    [InlineData(EProjectType.Standard)]
    [InlineData(EProjectType.IsPackage)]
    [InlineData(EProjectType.IsDbTools)]
    public void Find_WhenTheProjectIsNotAService_MarksItsPlacesAndLinksToTheProjectMenu(EProjectType projectType)
    {
        // Arrange
        _parameters.Projects["App"] = Project("G1", projectType, null, ["Prod"]);

        // Act
        ApiClientUsage result = Assert.Single(ApiClientUsages.Find(_parameters, ApiClientName));

        // Assert
        Assert.Equal(
            new ApiClientUsage($"G1/App/Srv1|Prod/Web Agent Name For Check (Project Type is {projectType})", "G1/App"),
            result);
    }

    //HideRemoteOnlyProjects leaves a remote project out of the menus; that mark wins over the project type
    [Fact]
    public void Find_WhenHideRemoteOnlyProjectsHidesTheProject_MarksItsPlacesAndLinksToTheParametersEditor()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;
        _parameters.Projects["Local"] = Project("G1", EProjectType.IsService, AppContext.BaseDirectory, ["Prod"]);
        _parameters.Projects["Remote"] = Project("G1", EProjectType.IsDbTools, MissingFolder(), ["Prod"]);

        // Act
        List<ApiClientUsage> result = ApiClientUsages.Find(_parameters, ApiClientName);

        // Assert
        Assert.Equal(
        [
            new ApiClientUsage("G1/Local/Srv1|Prod/Web Agent Name For Check", "G1/Local/Srv1|Prod"),
            new ApiClientUsage("G1/Remote/Srv1|Prod/Web Agent Name For Check (Hide Remote Only Projects is on)",
                EditorLink)
        ], result);
    }

    //a project without a group is in the group menu that the projects list names for it
    [Fact]
    public void Find_WhenTheProjectHasNoGroup_LinksThroughTheNoGroupMenu()
    {
        // Arrange
        _parameters.Projects["App"] = ServiceProject(null, ["Prod"]);

        // Act
        ApiClientUsage result = Assert.Single(ApiClientUsages.Find(_parameters, ApiClientName));

        // Assert
        Assert.Equal($"{SupportToolsParameters.FixProjectGroupName(null)}/App/Srv1|Prod", result.MenuLink);
    }

    //the way CliAppLoop.GoToMenu follows a link through the real menus; every link ends in a menu whose field (the end
    //of the place name) shows the client. The group BaGetter has the name of a main menu command, as in the real
    //parameters
    [Fact]
    public async Task Find_EveryMenuLinkLeadsToTheMenuThatShowsTheClient()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _parameters.LocalPackageManagerWebApiClientName = ApiClientName;
        _parameters.DatabaseServerConnections["Conn"] = WebAgentConnection(ApiClientName);
        _parameters.Servers["Srv1"] = new ServerDataModel
        {
            WebAgentName = ApiClientName, WebAgentInstallerName = ApiClientName
        };
        _parameters.Projects["App"] = ServiceProject("BaGetter", ["Prod"]);
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        List<ApiClientUsage> usages = ApiClientUsages.Find(_parameters, ApiClientName);

        // Assert
        Assert.Equal(6, usages.Count);
        foreach (ApiClientUsage usage in usages)
        {
            Assert.StartsWith($"{usage.MenuLink}/", usage.Name, StringComparison.Ordinal);
            Assert.Equal(FieldStatus,
                await GetMenuItemStatus(serviceProvider, usage.MenuLink, usage.Name[(usage.MenuLink.Length + 1)..]));
        }
    }

    //a hidden place links to the menu of the parameter that hides it
    [Fact]
    public async Task Find_HiddenPlaceLinksLeadToTheParametersThatHideThem()
    {
        // Arrange
        _parameters.DatabaseServerConnections["Conn"] = new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.SqlServer, DbWebAgentName = ApiClientName
        };
        _parameters.Projects["Db"] = Project("G1", EProjectType.IsDbTools, null, ["Prod"]);
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        List<ApiClientUsage> usages = ApiClientUsages.Find(_parameters, ApiClientName);

        // Assert
        Assert.Equal(2, usages.Count);
        Assert.Equal(nameof(EDatabaseProvider.SqlServer),
            await GetMenuItemStatus(serviceProvider, usages[0].MenuLink, "Database Server Provider"));
        Assert.Equal(nameof(EProjectType.IsDbTools),
            await GetMenuItemStatus(serviceProvider, usages[1].MenuLink, "Project Type"));
    }

    [Fact]
    public async Task Find_HiddenRemoteProjectLinkLeadsToTheHideRemoteOnlyProjectsParameter()
    {
        // Arrange
        _parameters.HideRemoteOnlyProjects = true;
        _parameters.Projects["Remote"] = Project("G1", EProjectType.IsService, MissingFolder(), ["Prod"]);
        // ReSharper disable once using
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        // Act
        ApiClientUsage usage = Assert.Single(ApiClientUsages.Find(_parameters, ApiClientName));

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

    private static DatabaseServerConnectionData WebAgentConnection(string? webAgentName)
    {
        return new DatabaseServerConnectionData
        {
            DatabaseServerProvider = EDatabaseProvider.WebAgent, DbWebAgentName = webAgentName
        };
    }

    private static ProjectModel ServiceProject(string? projectGroupName, string[] environmentNames)
    {
        return Project(projectGroupName, EProjectType.IsService, null, environmentNames);
    }

    //a project whose servers (all on Srv1) check with the client
    private static ProjectModel Project(string? projectGroupName, EProjectType projectType, string? projectFolderName,
        string[] environmentNames)
    {
        var project = new ProjectModel
        {
            ProjectGroupName = projectGroupName, ProjectType = projectType, ProjectFolderName = projectFolderName
        };
        foreach (string environmentName in environmentNames)
        {
            project.ServerInfos[$"k{environmentName}"] = new ServerInfoModel
            {
                ServerName = "Srv1", EnvironmentName = environmentName, WebAgentNameForCheck = ApiClientName
            };
        }

        return project;
    }

    private static string MissingFolder()
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }
}
