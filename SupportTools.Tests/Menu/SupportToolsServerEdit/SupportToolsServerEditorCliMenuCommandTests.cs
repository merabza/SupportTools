using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliMenu.CliMenuCommands;
using LibSupportToolsServerWork.CliMenuCommands;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SupportToolsServerEdit;
using Xunit;

namespace SupportTools.Tests.Menu.SupportToolsServerEdit;

[Collection(ConsoleCaptureCollection.Name)]
public sealed class SupportToolsServerEditorCliMenuCommandTests
{
    //the editors get the server settings and the HTTP client of the Support Tools Server Editor
    [Fact]
    public void GetSubMenu_WhenEditorConfigFileTypesIsOpened_ListsTheServerRecords()
    {
        // Arrange
        using var environment = new EditorConfigTestEnvironment();
        environment.Parameters.SupportToolsServerWebApiClientName = "SupportToolsServer";
        environment.Parameters.ApiClients["SupportToolsServer"] =
            new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""[{"Name":"default","Content":"root = true"}]""", Encoding.UTF8,
                    "application/json")
            });
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler.Object, false));
        var sut = new SupportToolsServerEditorCliMenuCommand(new Mock<ILogger>().Object, httpClientFactory.Object,
            new Mock<IMemoryCache>().Object, environment.ParametersManager.Object);
        var editorConfigFileTypes = Assert.IsType<EditorConfigFileTypesStsCliMenuCommand>(
            CliMenuTestAccess.GetMenuItems(sut.GetSubMenu())[1].CliMenuCommand);

        // Act
        CliMenuSet listMenu = editorConfigFileTypes.GetSubMenu();

        // Assert
        Assert.Contains(CliMenuTestAccess.GetMenuItems(listMenu), x => x.MenuItemName == "default");
    }

    [Fact]
    public void GetSubMenu_WhenCalled_ListsTheServerEditorsWithEditorConfigFileTypesAfterGitIgnoreFileTypes()
    {
        // Arrange
        var sut = new SupportToolsServerEditorCliMenuCommand(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, new Mock<IMemoryCache>().Object,
            new Mock<IParametersManager>().Object);

        // Act
        CliMenuSet subMenu = sut.GetSubMenu();

        // Assert
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(subMenu);
        Assert.Equal(["GitIgnore File Types", "EditorConfig File Types", "Gits from SupportToolsServer"],
            menuItems.Take(3).Select(x => x.MenuItemName));
        var editorConfigFileTypesCommand =
            Assert.IsType<EditorConfigFileTypesStsCliMenuCommand>(menuItems[1].CliMenuCommand);
        Assert.Equal(EMenuAction.LoadSubMenu, editorConfigFileTypesCommand.MenuActionOnBodySuccess);
    }

    [Fact]
    public void GetSubMenu_WhenCalled_EndsWithTheExitCommand()
    {
        // Arrange
        var sut = new SupportToolsServerEditorCliMenuCommand(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, new Mock<IMemoryCache>().Object,
            new Mock<IParametersManager>().Object);

        // Act
        CliMenuSet subMenu = sut.GetSubMenu();

        // Assert
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(subMenu);
        Assert.Equal(4, menuItems.Count);
        Assert.IsType<ExitToMainMenuCliMenuCommand>(menuItems[3].CliMenuCommand);
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndLoadsSubMenu()
    {
        // Act
        var sut = new SupportToolsServerEditorCliMenuCommand(new Mock<ILogger>().Object,
            new Mock<IHttpClientFactory>().Object, new Mock<IMemoryCache>().Object,
            new Mock<IParametersManager>().Object);

        // Assert
        Assert.Equal("Support Tools Server Editor", sut.Name);
        Assert.Equal(EMenuAction.LoadSubMenu, sut.MenuActionOnBodySuccess);
    }
}
