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
using SupportTools.CliMenuCommands;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SupportToolsServerApiContracts.V1.Requests;
using Xunit;

namespace SupportTools.Tests.CliMenuCommands;

//client patterns: CSharp, React. server patterns: CSharp, Vue
//client gits: AppCliTools (differs from server by folder name), NewRepo (only on client), Broken (only on client,
//not fully filled), Local (only on client, pattern missing on server, used by a project)
//server gits: AppCliTools, OldRepo (only on server, pattern in other case), Front (only on server, pattern missing on
//client)
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncGitProjectsCliMenuCommandTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";

    //the upload and the deletions also start the message hub, which really connects: on port 0 it fails at once
    private const string Server = "http://127.0.0.1:0/api/v1";

    private const string GitReposPath = "GET /api/v1/git/gitrepos";
    private const string PatternsPath = "GET /api/v1/git/gitignorefiletypeslist";
    private const string UploadPath = "POST /api/v1/git/uploadgitrepos";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly RoutingHttpMessageHandler _server = new();
    private int _selectedProcess;
    private CliMenuSet? _shownMenuSet;

    public SyncGitProjectsCliMenuCommandTests()
    {
        _parameters = new SupportToolsParameters
        {
            GitIgnorePatterns = { "CSharp", "React" }, SupportToolsServerWebApiClientName = ApiClientName
        };
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = Server };
        _parameters.Gits["AppCliTools"] = Git("git@github.com:x/AppCliTools.git", "AppCliTools", "CSharp");
        _parameters.Gits["NewRepo"] = Git("git@github.com:x/NewRepo.git", "NewRepo", "CSharp");
        _parameters.Gits["Broken"] = Git(null, "Broken", "CSharp");
        _parameters.Gits["Local"] = Git("git@github.com:x/Local.git", "Local", "React");
        _parameters.Projects["MyProject"] = new ProjectModel { ScaffoldSeederGitProjectNames = ["Local"] };

        RespondWithServerGits(ServerGit("AppCliTools", "git@github.com:x/AppCliTools.git", "AppCliToolsOld", "CSharp"),
            ServerGit("OldRepo", "git@github.com:x/OldRepo.git", "OldRepo", "csharp"),
            ServerGit("Front", "git@github.com:x/Front.git", "Front", "Vue"));
        _server.Respond(PatternsPath, HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<StsGitIgnoreFileTypeDataModel>
            {
                new() { Id = Guid.NewGuid(), Name = "CSharp", Content = "bin/\r\n" },
                new() { Id = Guid.NewGuid(), Name = "Vue", Content = "dist/\r\n" }
            }));

        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));

        _originalConsoleOutput = Console.Out;
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        Console.SetOut(_originalConsoleOutput);
        _consoleOutput.Dispose();
        _server.Dispose();
    }

    [Fact]
    public void Constructor_WhenCreated_SetsMenuNameAndReloadsMenuAfterRun()
    {
        // Act
        var sut = new SyncGitProjectsCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _parametersManager.Object);

        // Assert
        Assert.Equal("Sync Git Projects With SupportToolsServer...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Fact]
    public async Task RunBody_WhenSupportToolsServerIsNotSpecified_ReturnsFalseWithoutCallingServer()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("supportToolsServerApiClient is null", ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Theory]
    [InlineData(GitReposPath)]
    [InlineData(PatternsPath)]
    public async Task RunBody_WhenAServerListFails_ReturnsFalseWithoutAsking(string failingRequest)
    {
        // Arrange
        _server.Respond(failingRequest, HttpStatusCode.BadRequest,
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
        _parameters.Gits.Clear();
        _parameters.Gits["AppCliTools"] = Git("git@github.com:x/AppCliTools.git", "AppCliTools", "csharp");
        RespondWithServerGits(ServerGit("appclitools", "git@github.com:x/AppCliTools.git", "AppCliTools", "CSharp"));

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
        Assert.Equal([
            "server: add NewRepo; update AppCliTools; cannot add Broken, Local",
            "server: add NewRepo; update AppCliTools; delete Front, OldRepo; cannot add Broken, Local",
            "client: add OldRepo; update AppCliTools; cannot add Front",
            "client: add OldRepo; update AppCliTools; delete Broken, NewRepo; cannot add Front; cannot delete (in use) Local"
        ], items.Select(x => x.CliMenuCommand.StatusString));
        Assert.Equal([
            "  AppCliTools: differs (folder name)",
            "  Broken: only on client, not fully filled - cannot be uploaded",
            "  Local: only on client, .gitignore pattern React is missing on server - cannot be uploaded, " +
            "used by projects: MyProject - cannot be deleted on client",
            "  NewRepo: only on client",
            "  Front: only on server, .gitignore pattern Vue is missing on client - cannot be downloaded",
            "  OldRepo: only on server"
        ], ConsoleLines().SkipWhile(x => !x.StartsWith("Differences", StringComparison.Ordinal)).Skip(1).Take(6));
    }

    [Fact]
    public async Task RunBody_WhenMergeUpSelected_UploadsOnlyPossibleNewAndChangedRecordsWithoutPatterns()
    {
        // Arrange
        _selectedProcess = 0;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        (string _, string? body) = Assert.Single(_server.Requests,
            x => !x.Request.StartsWith("GET", StringComparison.Ordinal));
        var sent = JsonSerializer.Deserialize<SyncGitRequest>(body!)!;
        Assert.Empty(sent.GitIgnoreFiles);
        Assert.Equal(["NewRepo", "AppCliTools"], sent.Gits.Select(x => x.GitProjectName));
        Assert.Equal("AppCliTools", sent.Gits[1].GitProjectFolderName);
        Assert.Contains("Git records were not uploaded to server: Broken, Local", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Contains("2 git records uploaded to server, 0 deleted from server", ConsoleText(),
            StringComparison.Ordinal);
    }

    //the addresses are unique on the server, so the deletions go first: a git renamed on the client can then be added
    [Fact]
    public async Task RunBody_WhenSyncUpSelected_DeletesServerOnlyRecordsBeforeTheUpload()
    {
        // Arrange
        _selectedProcess = 1;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal([
            "DELETE /api/v1/git/deletegitrepo/Front", "DELETE /api/v1/git/deletegitrepo/OldRepo", UploadPath
        ], _server.Requests.Select(x => x.Request).Where(x => !x.StartsWith("GET", StringComparison.Ordinal)));
        Assert.Contains("2 git records uploaded to server, 2 deleted from server", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenServerRefusesTheUpload_ReturnsFalseAndShowsTheError()
    {
        // Arrange
        _selectedProcess = 0;
        _server.Respond(UploadPath, HttpStatusCode.Conflict,
            """{"title":"GitAddressIsInUse","status":409,"detail":"Git Address Is Used By AppCliTools"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("Git Address Is Used By AppCliTools", ConsoleText(), StringComparison.Ordinal);
        Assert.DoesNotContain("uploaded to server,", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenServerRefusesADeletion_ReturnsFalseWithoutUploading()
    {
        // Arrange
        _selectedProcess = 1;
        _server.Respond("DELETE /api/v1/git/deletegitrepo/Front", HttpStatusCode.NotFound,
            """{"title":"GitWithKeyNotFound","status":404,"detail":"Git With Key Front Not Found"}""");

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.Contains("Git With Key Front Not Found", ConsoleText(), StringComparison.Ordinal);
        Assert.DoesNotContain(_server.Requests, x => x.Request == UploadPath);
    }

    [Fact]
    public async Task RunBody_WhenMergeDownSelected_WritesPossibleServerRecordsAndKeepsExtraClientRecords()
    {
        // Arrange
        _selectedProcess = 2;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(["AppCliTools", "NewRepo", "Broken", "Local", "OldRepo"], _parameters.Gits.Keys);
        Assert.Equal("AppCliToolsOld", _parameters.Gits["AppCliTools"].GitProjectFolderName);
        GitDataModel oldRepo = _parameters.Gits["OldRepo"];
        Assert.Equal("git@github.com:x/OldRepo.git", oldRepo.GitProjectAddress);
        Assert.Equal("OldRepo", oldRepo.GitProjectFolderName);
        //the client compares pattern names case-sensitively, so the client's spelling is kept
        Assert.Equal("CSharp", oldRepo.GitIgnorePatternName);
        _parametersManager.Verify(
            x => x.Save(_parameters, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Contains("Git records were not downloaded from server: Front", ConsoleText(), StringComparison.Ordinal);
        Assert.DoesNotContain(_server.Requests, x => !x.Request.StartsWith("GET", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunBody_WhenSyncDownSelected_AlsoDeletesOnlyClientRecordsNotUsedByProjects()
    {
        // Arrange
        _selectedProcess = 3;

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.True(result);
        Assert.Equal(["AppCliTools", "Local", "OldRepo"], _parameters.Gits.Keys);
        Assert.Contains("Git records used by projects were not deleted from client: Local", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Contains("2 git records downloaded from server, 2 deleted from client", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenParametersAreNotSaved_ReturnsFalse()
    {
        // Arrange
        _selectedProcess = 2;
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        bool result = await CliMenuTestAccess.InvokeRunBody(CreateSut());

        // Assert
        Assert.False(result);
        Assert.DoesNotContain("downloaded from server,", ConsoleText(), StringComparison.Ordinal);
    }

    private SyncGitProjectsCliMenuCommand CreateSut()
    {
        return new SyncGitProjectsCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _parametersManager.Object, InputIdFromMenuList);
    }

    private int InputIdFromMenuList(string fieldName, CliMenuSet listSet)
    {
        _shownMenuSet = listSet;
        return _selectedProcess;
    }

    private void RespondWithServerGits(params StsGitDataModel[] gits)
    {
        _server.Respond(GitReposPath, HttpStatusCode.OK, JsonSerializer.Serialize(gits));
    }

    private static GitDataModel Git(string? address, string folderName, string gitIgnorePatternName)
    {
        return new GitDataModel
        {
            GitProjectAddress = address,
            GitProjectFolderName = folderName,
            GitIgnorePatternName = gitIgnorePatternName
        };
    }

    private static StsGitDataModel ServerGit(string name, string address, string folderName,
        string gitIgnorePatternName)
    {
        return new StsGitDataModel
        {
            GitProjectName = name,
            GitProjectAddress = address,
            GitProjectFolderName = folderName,
            GitIgnorePatternName = gitIgnorePatternName
        };
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    private string[] ConsoleLines()
    {
        return ConsoleText().Split(Environment.NewLine);
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
