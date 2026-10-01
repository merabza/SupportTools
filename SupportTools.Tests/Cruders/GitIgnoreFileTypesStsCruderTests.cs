using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

//the menu creates a new cruder for every list, but all of them share the application's memory cache
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitIgnoreFileTypesStsCruderTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ListRequest = "GET /api/v1/git/gitignorefiletypeslist";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly RoutingHttpMessageHandler _server = new();

    public GitIgnoreFileTypesStsCruderTests()
    {
        _parameters = new SupportToolsParameters { SupportToolsServerWebApiClientName = ApiClientName };
        //DELETE starts the message hub, which really connects to the server: on port 0 it fails at once
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        ServerLists("CSharp");
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _memoryCache.Dispose();
        _server.Dispose();
    }

    //the server records also change through other commands (Sync .gitignore files, uploading gits)
    [Fact]
    public void GetListMenu_WhenServerChangedSinceThePreviousList_ListsTheCurrentServerRecords()
    {
        // Arrange
        CreateSut().GetListMenu();
        ServerLists("CSharp", "React");

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal(["CSharp", "React"], ListedRecords(listMenu));
    }

    //the gitignore type of a server git is chosen from GetKeys (GitIgnorePathNameStsFieldEditor)
    [Fact]
    public void GetKeys_WhenServerChangedSinceThePreviousList_ListsTheCurrentServerRecords()
    {
        // Arrange
        CreateSut().GetListMenu();
        ServerLists("CSharp", "React");

        // Act
        List<string> keys = CreateSut().GetKeys();

        // Assert
        Assert.Equal(["CSharp", "React"], keys);
    }

    [Fact]
    public void ContainsRecordWithKey_WhenListIsShown_UsesTheDownloadedList()
    {
        // Arrange
        GitIgnoreFileTypesStsCruder sut = CreateSut();
        sut.GetListMenu();

        // Act
        bool result = sut.ContainsRecordWithKey("CSharp");

        // Assert
        Assert.True(result);
        Assert.Equal([ListRequest], _server.Requests.Select(x => x.Request));
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenCalled_SendsTheNameAndReloadsTheList()
    {
        // Arrange
        GitIgnoreFileTypesStsCruder sut = CreateSut();
        sut.GetListMenu();
        ServerLists("CSharp", "React");

        // Act
        await sut.UpdateRecordWithKey("React", new TextItemData { Text = "React" });

        // Assert
        Assert.True(sut.ContainsRecordWithKey("React"));
        Assert.Equal([ListRequest, "POST /api/v1/git/updategitignorefiletype/React", ListRequest],
            _server.Requests.Select(x => x.Request));
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenCalled_DeletesTheNameAndReloadsTheList()
    {
        // Arrange
        GitIgnoreFileTypesStsCruder sut = CreateSut();
        sut.GetListMenu();
        ServerLists();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "CSharp");

        // Assert
        Assert.False(sut.ContainsRecordWithKey("CSharp"));
        Assert.Equal([ListRequest, "DELETE /api/v1/git/deletegitignorefiletype/CSharp", ListRequest],
            _server.Requests.Select(x => x.Request));
    }

    //the list is built in GetSubMenu: an exception there would end the whole application
    [Fact]
    public void GetListMenu_WhenServerAddressIsInvalid_ListsNothingWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Empty(ListedRecords(listMenu));
        Assert.Contains("Invalid URI", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetListMenu_WhenNamedApiClientDoesNotExist_ListsNothingWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients.Remove(ApiClientName);

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Empty(ListedRecords(listMenu));
        Assert.Contains($"ApiClient with name {ApiClientName} does not exists", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public void Create_WhenCalled_AsksNewRecordsForTheText()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        FieldEditor fieldEditor = Assert.Single(fieldEditors);
        Assert.Equal(nameof(TextItemData.Text), fieldEditor.PropertyName);
        Assert.True(fieldEditor.EnterFieldDataOnCreate);
    }

    [Fact]
    public void GetListMenu_WhenSupportToolsServerIsNotSpecified_ListsNothingWithoutCallingServer()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Empty(ListedRecords(listMenu));
        string console = _consoleOutput.ToString();
        Assert.Contains("supportToolsServerWebApiClientName does not specified", console, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(NullReferenceException), console, StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public void GetListMenu_WhenCalled_NamesTheListAndTheNewCommand()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal("GitIgnore File Types", listMenu.Caption);
        Assert.Equal("New GitIgnore File Type", CliMenuTestAccess.GetMenuItems(listMenu)[0].MenuItemName);
    }

    [Fact]
    public void GetListMenu_WhenServerListFails_ShowsTheErrorAndListsNothing()
    {
        // Arrange
        _server.Respond(ListRequest, HttpStatusCode.BadRequest,
            """{"title":"SomeError","status":400,"detail":"Server list is broken"}""");

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Empty(ListedRecords(listMenu));
        string console = _consoleOutput.ToString();
        Assert.Contains("could not received GitIgnore File Types List", console, StringComparison.Ordinal);
        Assert.Contains("Server list is broken", console, StringComparison.Ordinal);
    }

    //the Text field of a record shows its name
    [Fact]
    public void FillDetailsSubMenu_WhenCalled_ShowsTheNameInTheTextField()
    {
        // Arrange
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(detailsMenu, "CSharp");

        // Assert
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(detailsMenu);
        Assert.Equal(["Record Name", "Text"], menuItems.Select(x => x.MenuItemName));
        menuItems[1].CliMenuCommand.CountStatus();
        Assert.Equal("CSharp", menuItems[1].CliMenuCommand.StatusString);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenSupportToolsServerIsNotSpecified_SendsNothing()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await CreateSut().UpdateRecordWithKey("React", new TextItemData { Text = "React" });

        // Assert
        string console = _consoleOutput.ToString();
        Assert.Contains("supportToolsServerApiClient is null", console, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(NullReferenceException), console, StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenServerRefusesTheName_PrintsTheError()
    {
        // Arrange
        _server.Respond("POST /api/v1/git/updategitignorefiletype/React", HttpStatusCode.BadRequest,
            """{"title":"ValueTooLong","status":400,"detail":"Name Is Longer Than 50 Characters"}""");

        // Act
        await CreateSut().UpdateRecordWithKey("React", new TextItemData { Text = "React" });

        // Assert
        Assert.Contains("Name Is Longer Than 50 Characters", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenServerAddressIsInvalid_PrintsTheExceptionWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        await CreateSut().UpdateRecordWithKey("React", new TextItemData { Text = "React" });

        // Assert
        Assert.Contains("Invalid URI", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenSupportToolsServerIsNotSpecified_DeletesNothing()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "CSharp");

        // Assert
        string console = _consoleOutput.ToString();
        Assert.Contains("supportToolsServerApiClient is null", console, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(NullReferenceException), console, StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenServerRefuses_PrintsTheError()
    {
        // Arrange
        _server.Respond("DELETE /api/v1/git/deletegitignorefiletype/CSharp", HttpStatusCode.Conflict,
            """{"title":"GitIgnoreFileTypeIsInUse","status":409,"detail":"GitIgnore File Type Is Used By Gits: CSharp (RepoA)"}""");

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "CSharp");

        // Assert
        Assert.Contains("GitIgnore File Type Is Used By Gits: CSharp (RepoA)", _consoleOutput.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenServerAddressIsInvalid_PrintsTheExceptionWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "CSharp");

        // Assert
        Assert.Contains("Invalid URI", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //renaming is deleting the old name and adding the new one
    [Fact]
    public async Task ChangeRecordKey_WhenCalled_DeletesTheOldNameAndAddsTheNewOne()
    {
        // Act
        bool result = await CreateSut().ChangeRecordKey("CSharp", "Python");

        // Assert
        Assert.True(result);
        Assert.Equal([
            ListRequest, "DELETE /api/v1/git/deletegitignorefiletype/CSharp",
            "POST /api/v1/git/updategitignorefiletype/Python"
        ], _server.Requests.Select(x => x.Request));
    }

    [Fact]
    public void CreateNewItem_WhenCalled_ReturnsAnEmptyTextItem()
    {
        // Act
        ItemData item = CliMenuTestAccess.InvokeCreateNewItem(CreateSut());

        // Assert
        Assert.Null(Assert.IsType<TextItemData>(item).Text);
    }

    private GitIgnoreFileTypesStsCruder CreateSut()
    {
        return GitIgnoreFileTypesStsCruder.Create(new Mock<ILogger>().Object, _httpClientFactory.Object, _memoryCache,
            _parametersManager.Object);
    }

    private void ServerLists(params string[] names)
    {
        _server.Respond(ListRequest, HttpStatusCode.OK,
            JsonSerializer.Serialize(names.Select(x => new StsGitIgnoreFileTypeDataModel
            {
                Id = Guid.NewGuid(), Name = x, Content = "bin/\r\n"
            }).ToList()));
    }

    private static List<string> ListedRecords(CliMenuSet listMenu)
    {
        return
        [
            .. CliMenuTestAccess.GetMenuItems(listMenu).Where(x => x.CliMenuCommand is ItemSubMenuCliMenuCommand)
                .Select(x => x.MenuItemName)
        ];
    }

    //remembers every request ("METHOD path" and body) and answers by "METHOD path" (200 without a body by default)
    private sealed class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = [];

        public List<(string Request, string? Body)> Requests { get; } = [];

        public void Respond(string request, HttpStatusCode statusCode, string body)
        {
            _responses[request] = (statusCode, body);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add((key,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

            if (!_responses.TryGetValue(key, out (HttpStatusCode StatusCode, string Body) response))
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
            }

            return new HttpResponseMessage(response.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(response.Body, Encoding.UTF8,
                    response.StatusCode == HttpStatusCode.OK ? "application/json" : "application/problem+json")
            };
        }
    }
}
