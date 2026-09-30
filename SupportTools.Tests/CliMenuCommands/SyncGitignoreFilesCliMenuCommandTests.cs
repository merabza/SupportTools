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
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands.GitIgnoreFileTypes;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

//client: CSharp (differs from server), React (only on client, not used), Local (only on client, used by a client git)
//server: CSharp, Old (only on server, not used), Used (only on server, used by a server git)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncGitignoreFilesCliMenuCommandTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";

    //the deletions also start the message hub, which really connects: on port 0 it fails at once
    private const string Server = "http://127.0.0.1:0/api/v1";

    private const string ListPath = "GET /api/v1/git/gitignorefiletypeslist";
    private const string GitReposPath = "GET /api/v1/git/gitrepos";
    private const string ClientCSharpContent = "bin/\r\nobj/\r\n";
    private const string ServerCSharpContent = "bin/\r\n";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly string _rootFolder;
    private readonly RoutingHttpMessageHandler _server = new();
    private CliMenuSet? _shownMenuSet;
    private int _selectedProcess;

    public SyncGitignoreFilesCliMenuCommandTests()
    {
        _rootFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
        File.WriteAllText(FilePath("CSharp"), ClientCSharpContent);
        File.WriteAllText(FilePath("React"), "node_modules/\r\n");
        File.WriteAllText(FilePath("Local"), "local/\r\n");

        _parameters = new SupportToolsParameters
        {
            FolderForGitignoreFiles = _rootFolder,
            GitIgnorePatterns = { "CSharp", "React", "Local" },
            SupportToolsServerWebApiClientName = ApiClientName
        };
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = Server };
        _parameters.Gits["LocalGit"] = new GitDataModel
        {
            GitProjectAddress = "git@github.com:x/local.git",
            GitProjectFolderName = "LocalGit",
            GitIgnorePatternName = "Local"
        };

        _server.Respond(ListPath, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsGitIgnoreFileTypeDataModel>
            {
                new() { Id = Guid.NewGuid(), Name = "CSharp", Content = ServerCSharpContent },
                new() { Id = Guid.NewGuid(), Name = "Old", Content = "old/\r\n" },
                new() { Id = Guid.NewGuid(), Name = "Used", Content = "used/\r\n" }
            }));
        _server.Respond(GitReposPath, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsGitDataModel>
            {
                new()
                {
                    GitProjectName = "ServerGit",
                    GitProjectAddress = "git@github.com:x/server.git",
                    GitProjectFolderName = "ServerGit",
                    GitIgnorePatternName = "Used"
                }
            }));

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager
            .Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_server, false));

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
        Directory.Delete(_rootFolder, true);
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        var sut = new SyncGitignoreFilesCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _parametersManager.Object);

        // Assert
        Assert.Equal("Sync .gitignore files...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task RunBody_WhenFolderForGitignoreFilesIsNotSpecified_ReturnsFalseWithoutCallingServer(
        string? folderForGitignoreFiles)
    {
        // Arrange
        _parameters.FolderForGitignoreFiles = folderForGitignoreFiles;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("FolderForGitignoreFiles is empty", ConsoleText(), StringComparison.Ordinal);
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
        Assert.Contains(FilePath("React"), ConsoleText(), StringComparison.Ordinal);
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
        Assert.Contains("Server list is broken", ConsoleText(), StringComparison.Ordinal);
        Assert.Null(_shownMenuSet);
    }

    [Fact]
    public async Task RunBody_WhenRecordsAreIdentical_ReturnsTrueWithoutAsking()
    {
        // Arrange
        _parameters.GitIgnorePatterns.Clear();
        _parameters.GitIgnorePatterns.Add("CSharp");
        _server.Respond(ListPath, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsGitIgnoreFileTypeDataModel>
            {
                new() { Id = Guid.NewGuid(), Name = "csharp", Content = ClientCSharpContent }
            }));

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Contains("identical", ConsoleText(), StringComparison.Ordinal);
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
            "server: add Local, React; update CSharp; delete Old; cannot delete (in use) Used",
            "client: add Old, Used; update CSharp",
            "client: add Old, Used; update CSharp; delete React; cannot delete (in use) Local"
        ], items.Select(x => x.CliMenuCommand.StatusString));
        string console = ConsoleText();
        Assert.Contains("CSharp: content differs", console, StringComparison.Ordinal);
        Assert.Contains("Local: only on client, used by client gits: LocalGit - cannot be deleted on client", console,
            StringComparison.Ordinal);
        Assert.Contains("Used: only on server, used by server gits: ServerGit - cannot be deleted on server", console,
            StringComparison.Ordinal);
        Assert.Contains("Old: only on server\n", console.Replace("\r", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenMergeUpSelected_UploadsNewAndChangedRecordsWithMergeAndDeletesNothing()
    {
        // Arrange
        _selectedProcess = 0;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        (string _, string? body) =
            Assert.Single(_server.Requests, x => x.Request.StartsWith("POST", StringComparison.Ordinal));
        List<StsGitIgnoreFileTypeDataModel> sent = JsonSerializer.Deserialize<List<StsGitIgnoreFileTypeDataModel>>(body!)!;
        Assert.Equal(["Local", "React", "CSharp"], sent.Select(x => x.Name));
        Assert.Equal(ClientCSharpContent, sent[2].Content);
        Assert.Contains(_server.Requests, x => x.Request == "POST /api/v1/git/syncupgitignorefiletypes/True");
        Assert.DoesNotContain(_server.Requests, x => x.Request.StartsWith("DELETE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunBody_WhenSyncUpSelected_AlsoDeletesOnlyUnusedServerRecords()
    {
        // Arrange
        _selectedProcess = 1;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(["DELETE /api/v1/git/deletegitignorefiletype/Old"],
            _server.Requests.Select(x => x.Request).Where(x => x.StartsWith("DELETE", StringComparison.Ordinal)));
        Assert.Contains("were not deleted from server: Used", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenServerRefusesTheDeletion_ReturnsFalseAndShowsTheError()
    {
        // Arrange
        _selectedProcess = 1;
        _server.Respond("DELETE /api/v1/git/deletegitignorefiletype/Old", HttpStatusCode.BadRequest,
            """{"title":"InUse","status":400,"detail":"Old is in use"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("Old is in use", ConsoleText(), StringComparison.Ordinal);
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
        Assert.Equal(["CSharp", "React", "Local", "Old", "Used"], _parameters.GitIgnorePatterns);
        Assert.Equal(ServerCSharpContent, await File.ReadAllTextAsync(FilePath("CSharp")));
        Assert.Equal("old/\r\n", await File.ReadAllTextAsync(FilePath("Old")));
        Assert.True(File.Exists(FilePath("React")));
        _parametersManager.Verify(
            x => x.Save(_parameters, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
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
        Assert.Equal(["CSharp", "Local", "Old", "Used"], _parameters.GitIgnorePatterns);
        Assert.False(File.Exists(FilePath("React")));
        Assert.True(File.Exists(FilePath("Local")));
        Assert.Contains("were not deleted from client: Local", ConsoleText(), StringComparison.Ordinal);
    }

    private SyncGitignoreFilesCliMenuCommand CreateSut()
    {
        return new SyncGitignoreFilesCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _parametersManager.Object, InputIdFromMenuList);
    }

    private int InputIdFromMenuList(string fieldName, CliMenuSet listSet)
    {
        _shownMenuSet = listSet;
        return _selectedProcess;
    }

    private string FilePath(string name)
    {
        return Path.Combine(_rootFolder, $"{name}.gitignore");
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
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
