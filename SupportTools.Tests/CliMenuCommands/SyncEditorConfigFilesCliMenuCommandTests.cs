using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

//client: CSharp (differs from server), React (only on client, not used), Local (only on client, used by a project)
//server: CSharp, Old (only on server)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncEditorConfigFilesCliMenuCommandTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ListPath = "GET /api/v1/git/editorconfigfiletypeslist";
    private const string ServerCSharpContent = "root = true\r\n";

    private readonly EditorConfigTestEnvironment _env = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly RoutingHttpMessageHandler _server = new();
    private int _selectedProcess;
    private CliMenuSet? _shownMenuSet;

    public SyncEditorConfigFilesCliMenuCommandTests()
    {
        File.WriteAllText(FilePath("React"), "[*.ts]\r\n");
        File.WriteAllText(FilePath("Local"), "[*.local]\r\n");
        _env.Parameters.EditorConfigPatterns.Add("React");
        _env.Parameters.EditorConfigPatterns.Add("Local");
        _env.AddProject("LocalProject", "Local", null);

        _env.Parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _env.Parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://localhost:5033/api/v1" };
        _server.Respond(ListPath, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsEditorConfigFileTypeDataModel>
            {
                new() { Name = EditorConfigTestEnvironment.PatternName, Content = ServerCSharpContent },
                new() { Name = "Old", Content = "[*.old]\r\n" }
            }));
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_server, false));
    }

    public void Dispose()
    {
        _server.Dispose();
        _env.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        var sut = new SyncEditorConfigFilesCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _env.ParametersManager.Object);

        // Assert
        Assert.Equal("Sync .editorconfig files...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task RunBody_WhenFolderForEditorConfigFilesIsNotSpecified_ReturnsFalseWithoutCallingServer(
        string? folderForEditorConfigFiles)
    {
        // Arrange
        _env.Parameters.FolderForEditorConfigFiles = folderForEditorConfigFiles;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("FolderForEditorConfigFiles is empty", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task RunBody_WhenAClientFileIsMissing_ReturnsFalseWithoutCallingServer()
    {
        // Arrange
        File.Delete(FilePath("React"));

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains(FilePath("React"), _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task RunBody_WhenSupportToolsServerIsNotSpecified_ReturnsFalseWithoutCallingServer()
    {
        // Arrange
        _env.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("supportToolsServerApiClient is null", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task RunBody_WhenServerListFails_ReturnsFalseWithoutAsking()
    {
        // Arrange
        _server.Respond(ListPath, HttpStatusCode.BadRequest,
            """{"title":"SomeError","status":400,"detail":"Server list is broken"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("Server list is broken", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Null(_shownMenuSet);
    }

    [Fact]
    public async Task RunBody_WhenRecordsAreIdentical_ReturnsTrueWithoutAsking()
    {
        // Arrange
        _env.Parameters.EditorConfigPatterns.RemoveAll(x => x != EditorConfigTestEnvironment.PatternName);
        _server.Respond(ListPath, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsEditorConfigFileTypeDataModel>
            {
                new() { Name = "csharp", Content = EditorConfigTestEnvironment.TemplateContent }
            }));

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("identical", _env.ConsoleText(), StringComparison.Ordinal);
        Assert.Null(_shownMenuSet);
    }

    [Fact]
    public async Task RunBody_WhenRecordsDiffer_ShowsEveryProcessWithItsChanges()
    {
        // Arrange
        _selectedProcess = -2;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        List<CliMenuItem> items = CliMenuTestAccess.GetMenuItems(_shownMenuSet!);
        items.ForEach(x => x.CliMenuCommand.CountStatus());
        Assert.Equal(["Merge Up", "Sync Up", "Merge Down", "Sync Down"], items.Select(x => x.MenuItemName));
        Assert.Equal(
        [
            "server: add Local, React; update CSharp",
            "server: add Local, React; update CSharp; delete Old",
            "client: add Old; update CSharp",
            "client: add Old; update CSharp; delete React; cannot delete (in use) Local"
        ], items.Select(x => x.CliMenuCommand.StatusString));
        string console = _env.ConsoleText();
        Assert.Contains("CSharp: content differs", console, StringComparison.Ordinal);
        Assert.Contains("Local: only on client, used by projects: LocalProject - cannot be deleted on client",
            console, StringComparison.Ordinal);
        Assert.Contains("Old: only on server", console, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenMergeUpSelected_UploadsOnlyNewAndChangedRecordsWithMerge()
    {
        // Arrange
        _selectedProcess = 0;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        (string request, string? body) = Assert.Single(_server.Requests, x => x.Request.StartsWith("POST",
            StringComparison.Ordinal));
        Assert.Equal("POST /api/v1/git/syncupeditorconfigfiletypes/True", request);
        List<StsEditorConfigFileTypeDataModel> sent =
            JsonSerializer.Deserialize<List<StsEditorConfigFileTypeDataModel>>(body!)!;
        Assert.Equal(["Local", "React", "CSharp"], sent.Select(x => x.Name));
        Assert.Equal(EditorConfigTestEnvironment.TemplateContent, sent[2].Content);
    }

    //without merge the server deletes every record missing from the upload, so the whole client list is sent
    [Fact]
    public async Task RunBody_WhenSyncUpSelected_UploadsEveryClientRecordWithoutMerge()
    {
        // Arrange
        _selectedProcess = 1;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        (string request, string? body) = Assert.Single(_server.Requests, x => x.Request.StartsWith("POST",
            StringComparison.Ordinal));
        Assert.Equal("POST /api/v1/git/syncupeditorconfigfiletypes/False", request);
        List<StsEditorConfigFileTypeDataModel> sent =
            JsonSerializer.Deserialize<List<StsEditorConfigFileTypeDataModel>>(body!)!;
        Assert.Equal([EditorConfigTestEnvironment.PatternName, "React", "Local"], sent.Select(x => x.Name));
        Assert.Contains("3 .editorconfig records uploaded to server, 1 deleted from server", _env.ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenServerRefusesTheUpload_ReturnsFalseAndShowsTheError()
    {
        // Arrange
        _selectedProcess = 1;
        _server.Respond("POST /api/v1/git/syncupeditorconfigfiletypes/False", HttpStatusCode.BadRequest,
            """{"title":"ValueTooLong","status":400,"detail":"CSharp.Content Is Longer Than 65536 Characters"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("CSharp.Content Is Longer Than 65536 Characters", _env.ConsoleText(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("uploaded to server", _env.ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenMergeDownSelected_WritesServerRecordsAndKeepsExtraClientRecords()
    {
        // Arrange
        _selectedProcess = 2;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([EditorConfigTestEnvironment.PatternName, "React", "Local", "Old"],
            _env.Parameters.EditorConfigPatterns);
        Assert.Equal(ServerCSharpContent, await File.ReadAllTextAsync(_env.TemplateFileName));
        Assert.Equal("[*.old]\r\n", await File.ReadAllTextAsync(FilePath("Old")));
        Assert.True(File.Exists(FilePath("React")));
        _env.ParametersManager.Verify(
            x => x.Save(_env.Parameters, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.DoesNotContain(_server.Requests, x => !x.Request.StartsWith("GET", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunBody_WhenSyncDownSelected_AlsoDeletesOnlyUnusedClientRecords()
    {
        // Arrange
        _selectedProcess = 3;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([EditorConfigTestEnvironment.PatternName, "Local", "Old"], _env.Parameters.EditorConfigPatterns);
        Assert.False(File.Exists(FilePath("React")));
        Assert.True(File.Exists(FilePath("Local")));
        Assert.Contains("were not deleted from client: Local", _env.ConsoleText(), StringComparison.Ordinal);
    }

    private SyncEditorConfigFilesCliMenuCommand CreateSut()
    {
        return new SyncEditorConfigFilesCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _env.ParametersManager.Object, InputIdFromMenuList);
    }

    private int InputIdFromMenuList(string fieldName, CliMenuSet listSet)
    {
        _shownMenuSet = listSet;
        return _selectedProcess;
    }

    private string FilePath(string name)
    {
        return Path.Combine(_env.TemplatesFolder, $"{name}.editorconfig");
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
