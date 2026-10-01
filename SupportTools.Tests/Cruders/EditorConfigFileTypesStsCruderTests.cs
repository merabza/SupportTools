using System;
using System.Collections.Generic;
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
using LibSupportToolsServerWork.FieldEditors;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

//server: default (with the content of the CSharp template) and BaGetter
[Collection(ConsoleCaptureCollection.Name)]
public sealed class EditorConfigFileTypesStsCruderTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ListRequest = "GET /api/v1/git/editorconfigfiletypeslist";
    private const string MergeUpRequest = "POST /api/v1/git/syncupeditorconfigfiletypes/True";
    private const string BaGetterContent = "[*.cs]\r\n";

    private readonly EditorConfigTestEnvironment _env = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly RoutingHttpMessageHandler _server = new();

    public EditorConfigFileTypesStsCruderTests()
    {
        _env.Parameters.SupportToolsServerWebApiClientName = ApiClientName;
        //DELETE starts the message hub, which really connects to the server: on port 0 it fails at once
        _env.Parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
        _server.Respond(ListRequest, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsEditorConfigFileTypeDataModel>
            {
                new() { Name = "default", Content = EditorConfigTestEnvironment.TemplateContent },
                new() { Name = "BaGetter", Content = BaGetterContent }
            }));
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));
    }

    public void Dispose()
    {
        _server.Dispose();
        _env.Dispose();
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsServerRecordsWithTheirContentLength()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal("EditorConfig File Types", listMenu.Caption);
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(listMenu);
        Assert.Equal(["New EditorConfig File Type", "BaGetter", "default"],
            menuItems.Take(3).Select(x => x.MenuItemName));
        menuItems.ForEach(x => x.CliMenuCommand.CountStatus());
        Assert.Equal($"{BaGetterContent.Length} characters", menuItems[1].CliMenuCommand.StatusString);
        Assert.Equal($"{EditorConfigTestEnvironment.TemplateContent.Length} characters",
            menuItems[2].CliMenuCommand.StatusString);
        //the statuses are counted from the list that is already downloaded
        Assert.Equal([ListRequest], _server.Requests.Select(x => x.Request));
    }

    [Fact]
    public void GetListMenu_WhenSupportToolsServerIsNotSpecified_ListsNothingWithoutCallingServer()
    {
        // Arrange
        _env.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(listMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
        Assert.Contains("supportToolsServerWebApiClientName does not specified", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    //the list is built in GetSubMenu: an exception there would end the whole application
    [Fact]
    public void GetListMenu_WhenServerAddressIsInvalid_ListsNothingWithoutThrowing()
    {
        // Arrange
        _env.Parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(listMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
        Assert.Contains("Invalid URI", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void GetListMenu_WhenNamedApiClientDoesNotExist_ListsNothingWithoutThrowing()
    {
        // Arrange
        _env.Parameters.ApiClients.Remove(ApiClientName);

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(listMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
        Assert.Contains($"ApiClient with name {ApiClientName} does not exists", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
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
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(listMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
        Assert.Contains("Server list is broken", _env.ConsoleText(), StringComparison.Ordinal);
    }

    //the server matches names ignoring case, so uploading "DEFAULT" would overwrite "default"
    [Theory]
    [InlineData("default", true)]
    [InlineData("DEFAULT", true)]
    [InlineData("React", false)]
    public void ContainsRecordWithKey_WhenCalled_MatchesServerNamesIgnoringCase(string recordKey, bool expected)
    {
        // Act
        bool result = CreateSut().ContainsRecordWithKey(recordKey);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FillDetailsSubMenu_WhenCalled_AddsRecordNameAndContentEditors()
    {
        // Arrange
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(detailsMenu, "BaGetter");

        // Assert
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(detailsMenu);
        Assert.Equal(["Record Name", "Content"], menuItems.Select(x => x.MenuItemName));
        menuItems[1].CliMenuCommand.CountStatus();
        Assert.Equal($"{BaGetterContent.Length} characters", menuItems[1].CliMenuCommand.StatusString);
    }

    //with merge the server adds or replaces only the uploaded record and leaves the others alone
    [Fact]
    public async Task UpdateRecordWithKey_WhenCalled_UploadsOnlyThatRecordWithMergeAndReloadsTheList()
    {
        // Arrange
        EditorConfigFileTypesStsCruder sut = CreateSut();
        sut.GetListMenu();

        // Act
        await sut.UpdateRecordWithKey("default", new TextItemData { Text = "root = true\r\n" });
        sut.GetStatusFor("default");

        // Assert
        Assert.Equal([ListRequest, MergeUpRequest, ListRequest], _server.Requests.Select(x => x.Request));
        StsEditorConfigFileTypeDataModel sent = Assert.Single(SentRecords(_server.Requests[1].Body));
        Assert.Equal("default", sent.Name);
        Assert.Equal("root = true\r\n", sent.Content);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenServerRefusesTheRecord_PrintsTheError()
    {
        // Arrange
        _server.Respond(MergeUpRequest, HttpStatusCode.BadRequest,
            """{"title":"ValueTooLong","status":400,"detail":"default.Content Is Longer Than 65536 Characters"}""");

        // Act
        await CreateSut().UpdateRecordWithKey("default", new TextItemData { Text = "root = true\r\n" });

        // Assert
        Assert.Contains("default.Content Is Longer Than 65536 Characters", _env.ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenCalled_DeletesTheEscapedNameAndReloadsTheList()
    {
        // Arrange
        EditorConfigFileTypesStsCruder sut = CreateSut();
        sut.GetListMenu();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "Ba Getter");
        sut.ContainsRecordWithKey("BaGetter");

        // Assert
        Assert.Equal([ListRequest, "DELETE /api/v1/git/deleteeditorconfigfiletype/Ba%20Getter", ListRequest],
            _server.Requests.Select(x => x.Request));
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenServerDoesNotKnowTheName_PrintsTheError()
    {
        // Arrange
        _server.Respond("DELETE /api/v1/git/deleteeditorconfigfiletype/React", HttpStatusCode.NotFound,
            """{"title":"EditorConfigFileTypeWithNameNotFound","status":404,"detail":"EditorConfig File Type With Name React Not Found"}""");

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "React");

        // Assert
        Assert.Contains("EditorConfig File Type With Name React Not Found", _env.ConsoleText(),
            StringComparison.Ordinal);
    }

    //renaming is deleting and adding: the content goes to the server under the new name
    [Fact]
    public async Task ChangeRecordKey_WhenCalled_DeletesTheOldNameAndUploadsItsContentUnderTheNewName()
    {
        // Act
        bool result = await CreateSut().ChangeRecordKey("BaGetter", "NuGetServer");

        // Assert
        Assert.True(result);
        Assert.Equal([ListRequest, "DELETE /api/v1/git/deleteeditorconfigfiletype/BaGetter", MergeUpRequest],
            _server.Requests.Select(x => x.Request));
        StsEditorConfigFileTypeDataModel sent = Assert.Single(SentRecords(_server.Requests[2].Body));
        Assert.Equal("NuGetServer", sent.Name);
        Assert.Equal(BaGetterContent, sent.Content);
    }

    //New asks for the content, which is loaded from a file
    [Fact]
    public void Create_WhenCalled_AsksNewRecordsForTheContentFromAFile()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        FieldEditor fieldEditor = Assert.Single(fieldEditors);
        Assert.IsType<EditorConfigContentStsFieldEditor>(fieldEditor);
        Assert.Equal(nameof(TextItemData.Text), fieldEditor.PropertyName);
        Assert.True(fieldEditor.EnterFieldDataOnCreate);
    }

    [Fact]
    public void GetStatusFor_WhenRecordDoesNotExist_IsEmpty()
    {
        // Act
        string status = CreateSut().GetStatusFor("React");

        // Assert
        Assert.Equal(string.Empty, status);
        Assert.DoesNotContain("[ERROR]", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenSupportToolsServerIsNotSpecified_SendsNothing()
    {
        // Arrange
        _env.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await CreateSut().UpdateRecordWithKey("default", new TextItemData { Text = "root = true\r\n" });

        // Assert
        Assert.Contains("supportToolsServerWebApiClientName does not specified", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    //the server accepts empty content, but not a missing one
    [Fact]
    public async Task UpdateRecordWithKey_WhenRecordHasNoContent_UploadsEmptyContent()
    {
        // Act
        await CreateSut().UpdateRecordWithKey("default", new TextItemData());

        // Assert
        StsEditorConfigFileTypeDataModel sent = Assert.Single(SentRecords(Assert.Single(_server.Requests).Body));
        Assert.Equal(string.Empty, sent.Content);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenSupportToolsServerIsNotSpecified_DeletesNothing()
    {
        // Arrange
        _env.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "default");

        // Assert
        Assert.Contains("supportToolsServerWebApiClientName does not specified", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public void CreateNewItem_WhenCalled_ReturnsAnItemWithoutContent()
    {
        // Act
        ItemData item = CliMenuTestAccess.InvokeCreateNewItem(CreateSut());

        // Assert
        Assert.Null(Assert.IsType<TextItemData>(item).Text);
    }

    private EditorConfigFileTypesStsCruder CreateSut()
    {
        return new EditorConfigFileTypesStsCruder(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _env.ParametersManager.Object);
    }

    private static List<StsEditorConfigFileTypeDataModel> SentRecords(string? body)
    {
        return JsonSerializer.Deserialize<List<StsEditorConfigFileTypeDataModel>>(body!)!;
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
