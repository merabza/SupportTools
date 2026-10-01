using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using LibGitData.Models;
using LibSupportToolsServerWork.Cruders;
using LibSupportToolsServerWork.FieldEditors;
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
public sealed class GitStsCruderTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ListRequest = "GET /api/v1/git/gitrepos";
    private const string RepoAAddress = "git@github.com:x/a.git";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters;
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly RoutingHttpMessageHandler _server = new();
    private string? _tempFolder;

    public GitStsCruderTests()
    {
        _parameters = new SupportToolsParameters { SupportToolsServerWebApiClientName = ApiClientName };
        //DELETE starts the message hub, which really connects to the server: on port 0 it fails at once
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
        _parametersManager.SetupGet(x => x.Parameters).Returns(_parameters);
        ServerLists(GitRepo("RepoA", RepoAAddress));
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
        if (_tempFolder is not null)
        {
            Directory.Delete(_tempFolder, true);
        }
    }

    //New asks for the fields entered on creation; the server refuses a git without its gitignore type, which
    //must be one of the server's types
    [Fact]
    public void Create_WhenCalled_AsksNewRecordsForAddressFolderAndServerGitIgnoreType()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateSut());

        // Assert
        Assert.Equal([
            nameof(GitDataModel.GitProjectAddress), nameof(GitDataModel.GitProjectFolderName),
            nameof(GitDataModel.GitIgnorePatternName)
        ], fieldEditors.Where(x => x.EnterFieldDataOnCreate).Select(x => x.PropertyName));
        Assert.IsType<GitIgnorePathNameStsFieldEditor>(fieldEditors[2]);
    }

    //the server records also change through other commands (Sync Git Projects, uploading gits)
    [Fact]
    public void GetListMenu_WhenServerChangedSinceThePreviousList_ListsTheCurrentServerRecords()
    {
        // Arrange
        CreateSut().GetListMenu();
        ServerLists(GitRepo("RepoA", RepoAAddress), GitRepo("RepoB", "git@github.com:x/b.git"));

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal(["RepoA", "RepoB"],
            CliMenuTestAccess.GetMenuItems(listMenu).Where(x => x.CliMenuCommand is ItemSubMenuCliMenuCommand)
                .Select(x => x.MenuItemName));
    }

    [Fact]
    public void GetListMenu_WhenStatusesAreCounted_UsesTheDownloadedList()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();
        CliMenuCommand repoA = CliMenuTestAccess.GetMenuItems(listMenu).Single(x => x.MenuItemName == "RepoA")
            .CliMenuCommand;
        repoA.CountStatus();

        // Assert
        Assert.Equal($"{RepoAAddress} Usage count is: 0", repoA.StatusString);
        Assert.Equal([ListRequest], _server.Requests.Select(x => x.Request));
    }

    //the server matches the keys ignoring case (the key requests answer as the server does)
    [Theory]
    [InlineData("RepoA", true)]
    [InlineData("REPOA", true)]
    [InlineData("RepoNew", false)]
    public void ContainsRecordWithKey_WhenCalled_MatchesServerKeysIgnoringCase(string recordKey, bool expected)
    {
        // Arrange
        RespondToKeyRequestsLikeServer();

        // Act
        bool result = CreateSut().ContainsRecordWithKey(recordKey);

        // Assert
        Assert.Equal(expected, result);
    }

    //New checks with it that the name is free: the expected "not found" must not be printed as an error
    [Fact]
    public void ContainsRecordWithKey_WhenNameIsFree_ReturnsFalseWithoutErrorsOrExtraRequests()
    {
        // Arrange
        RespondToKeyRequestsLikeServer();
        GitStsCruder sut = CreateSut();
        sut.GetListMenu();

        // Act
        bool result = sut.ContainsRecordWithKey("RepoNew");

        // Assert
        Assert.False(result);
        Assert.DoesNotContain("[ERROR]", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal([ListRequest], _server.Requests.Select(x => x.Request));
    }

    //after an edit the record is read from the list again (its status, the defaults of the next edit),
    //so the list must hold the saved values
    [Fact]
    public async Task UpdateRecordWithKey_WhenCalled_SendsTheRecordAndReloadsTheList()
    {
        // Arrange
        GitStsCruder sut = CreateSut();
        sut.GetListMenu();
        ServerLists(GitRepo("RepoA", "git@github.com:x/new.git"));

        // Act
        await sut.UpdateRecordWithKey("RepoA",
            new GitDataModel
            {
                GitProjectAddress = "git@github.com:x/new.git",
                GitProjectFolderName = "A",
                GitIgnorePatternName = "CSharp"
            });

        // Assert
        Assert.Equal("git@github.com:x/new.git Usage count is: 0", sut.GetStatusFor("RepoA"));
        Assert.Equal([ListRequest, "POST /api/v1/git/updategitrepo/RepoA", ListRequest],
            _server.Requests.Select(x => x.Request));
        var sent = JsonSerializer.Deserialize<StsGitDataModel>(_server.Requests[1].Body!)!;
        Assert.Equal("RepoA", sent.GitProjectName);
        Assert.Equal("git@github.com:x/new.git", sent.GitProjectAddress);
        Assert.Equal("A", sent.GitProjectFolderName);
        Assert.Equal("CSharp", sent.GitIgnorePatternName);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenCalled_DeletesTheRepoAndReloadsTheList()
    {
        // Arrange
        GitStsCruder sut = CreateSut();
        sut.GetListMenu();
        ServerLists();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");

        // Assert
        Assert.Equal("ERROR: Git address Not found", sut.GetStatusFor("RepoA"));
        Assert.Equal([ListRequest, "DELETE /api/v1/git/deletegitrepo/RepoA", ListRequest],
            _server.Requests.Select(x => x.Request));
    }

    //the list is built in GetSubMenu: an exception there would end the whole application
    [Fact]
    public void GetListMenu_WhenNamedApiClientDoesNotExist_ListsNothingWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients.Remove(ApiClientName);

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(listMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
        Assert.Contains($"ApiClient with name {ApiClientName} does not exists", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public void GetListMenu_WhenCalled_ListsOnlyTheNewCommandTheRecordsAndTheExit()
    {
        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.Equal("GitsFromServer", listMenu.Caption);
        List<CliMenuItem> menuItems = CliMenuTestAccess.GetMenuItems(listMenu);
        Assert.Equal(3, menuItems.Count);
        Assert.Equal(["New GitFromServer", "RepoA"], menuItems.Take(2).Select(x => x.MenuItemName));
    }

    [Fact]
    public void GetListMenu_WhenSupportToolsServerIsNotSpecified_ListsNothingWithoutCallingServer()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        CliMenuSet listMenu = CreateSut().GetListMenu();

        // Assert
        Assert.DoesNotContain(CliMenuTestAccess.GetMenuItems(listMenu),
            x => x.CliMenuCommand is ItemSubMenuCliMenuCommand);
        string console = _consoleOutput.ToString();
        Assert.Contains("supportToolsServerWebApiClientName does not specified", console, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(NullReferenceException), console, StringComparison.Ordinal);
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
        string console = _consoleOutput.ToString();
        Assert.Contains("could not received remoteGits", console, StringComparison.Ordinal);
        Assert.Contains("Server list is broken", console, StringComparison.Ordinal);
    }

    //a git is used by projects directly and as a scaffold seeder git
    [Fact]
    public void GetStatusFor_WhenProjectsUseTheGit_CountsBothKindsOfUsage()
    {
        // Arrange
        _parameters.Projects["Direct"] = new ProjectModel { GitProjectNames = ["RepoA"] };
        _parameters.Projects["Seeder"] = new ProjectModel { ScaffoldSeederGitProjectNames = ["RepoA", "RepoB"] };
        _parameters.Projects["Other"] = new ProjectModel { GitProjectNames = ["RepoB"] };

        // Act
        string status = CreateSut().GetStatusFor("RepoA");

        // Assert
        Assert.Equal($"{RepoAAddress} Usage count is: 2", status);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenSupportToolsServerIsNotSpecified_SendsNothing()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await CreateSut().UpdateRecordWithKey("RepoA", NewRepoA());

        // Assert
        string console = _consoleOutput.ToString();
        Assert.Contains("supportToolsServerApiClient is null", console, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(NullReferenceException), console, StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenRecordIsNotAGit_SendsNothing()
    {
        // Act
        await CreateSut().UpdateRecordWithKey("RepoA", new TextItemData());

        // Assert
        Assert.Contains("newRecord is not GitDataModel", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Theory]
    [InlineData(null, "A", "CSharp", "GitProjectAddress is not entered")]
    [InlineData(" ", "A", "CSharp", "GitProjectAddress is not entered")]
    [InlineData(RepoAAddress, null, "CSharp", "GitProjectFolderName is not entered")]
    [InlineData(RepoAAddress, " ", "CSharp", "GitProjectFolderName is not entered")]
    [InlineData(RepoAAddress, "A", null, "GitIgnorePatternName is not entered")]
    [InlineData(RepoAAddress, "A", " ", "GitIgnorePatternName is not entered")]
    public async Task UpdateRecordWithKey_WhenARequiredValueIsMissing_SendsNothing(string? address, string? folder,
        string? pattern, string expectedError)
    {
        // Act
        await CreateSut().UpdateRecordWithKey("RepoA",
            new GitDataModel
            {
                GitProjectAddress = address, GitProjectFolderName = folder, GitIgnorePatternName = pattern
            });

        // Assert
        Assert.Contains(expectedError, _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenServerRefusesTheRecord_PrintsTheError()
    {
        // Arrange
        _server.Respond("POST /api/v1/git/updategitrepo/RepoA", HttpStatusCode.Conflict,
            """{"title":"GitAddressIsInUse","status":409,"detail":"Git Address git@github.com:x/a.git Is Used By RepoB"}""");

        // Act
        await CreateSut().UpdateRecordWithKey("RepoA", NewRepoA());

        // Assert
        Assert.Contains("Is Used By RepoB", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenServerAddressIsInvalid_PrintsTheExceptionWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        await CreateSut().UpdateRecordWithKey("RepoA", NewRepoA());

        // Assert
        Assert.Contains("Invalid URI", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //renaming is deleting and adding: the record goes to the server under the new key with all its values
    [Fact]
    public async Task ChangeRecordKey_WhenCalled_DeletesTheOldKeyAndSendsTheRecordUnderTheNewKey()
    {
        // Act
        bool result = await CreateSut().ChangeRecordKey("RepoA", "RepoB");

        // Assert
        Assert.True(result);
        Assert.Equal([ListRequest, "DELETE /api/v1/git/deletegitrepo/RepoA", "POST /api/v1/git/updategitrepo/RepoB"],
            _server.Requests.Select(x => x.Request));
        var sent = JsonSerializer.Deserialize<StsGitDataModel>(_server.Requests[2].Body!)!;
        Assert.Equal("RepoB", sent.GitProjectName);
        Assert.Equal(RepoAAddress, sent.GitProjectAddress);
        Assert.Equal("RepoA", sent.GitProjectFolderName);
        Assert.Equal("CSharp", sent.GitIgnorePatternName);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenCalled_AlsoRemovesTheLocalGit()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel { GitProjectAddress = RepoAAddress };
        _parameters.Gits["RepoB"] = new GitDataModel();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "RepoA");

        // Assert
        Assert.Equal(["RepoB"], _parameters.Gits.Keys);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenSupportToolsServerIsNotSpecified_DeletesNothing()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = null;
        _parameters.Gits["RepoA"] = new GitDataModel();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "RepoA");

        // Assert
        Assert.Contains("supportToolsServerApiClient is null", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
        Assert.True(_parameters.Gits.ContainsKey("RepoA"));
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenServerRefuses_PrintsTheError()
    {
        // Arrange
        _server.Respond("DELETE /api/v1/git/deletegitrepo/RepoA", HttpStatusCode.NotFound,
            """{"title":"GitWithKeyNotFound","status":404,"detail":"Git With Key RepoA Not Found"}""");

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "RepoA");

        // Assert
        Assert.Contains("Git With Key RepoA Not Found", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenServerAddressIsInvalid_PrintsTheExceptionWithoutThrowing()
    {
        // Arrange
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateSut(), "RepoA");

        // Assert
        Assert.Contains("Invalid URI", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CheckValidation_WhenItemIsNotAGit_ReturnsFalse()
    {
        // Act
        bool result = CreateSut().CheckValidation(new TextItemData());

        // Assert
        Assert.False(result);
        Assert.Contains("item is not GitDataModel", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CheckValidation_WhenAddressIsMissing_ReturnsFalse()
    {
        // Act
        bool result = CreateSut().CheckValidation(new GitDataModel());

        // Assert
        Assert.False(result);
        Assert.Contains("GitProjectAddress is null", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    //the address is checked with git ls-remote; a local repository answers without network
    [Fact]
    public void CheckValidation_WhenAddressIsARepository_ReturnsTrue()
    {
        // Arrange
        string repository = CreateBareRepository();

        // Act
        bool result = CreateSut().CheckValidation(new GitDataModel { GitProjectAddress = repository });

        // Assert
        Assert.True(result);
        Assert.Contains($"ls-remote {repository}", _consoleOutput.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CheckValidation_WhenAddressIsNotARepository_ReturnsFalse()
    {
        // Arrange
        string missingRepository = Path.Combine(TempFolder(), "missing.git");

        // Act
        bool result = CreateSut().CheckValidation(new GitDataModel { GitProjectAddress = missingRepository });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckValidation_WhenGitCannotBeStarted_LogsTheErrorAndReturnsFalse()
    {
        // Arrange
        _parameters.GitExecutablePath = Path.Combine(TempFolder(), "missing-git.exe");

        // Act
        bool result = CreateSut().CheckValidation(new GitDataModel { GitProjectAddress = RepoAAddress });

        // Assert
        Assert.False(result);
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == "Error occurred while validating GitDataModel"),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void CreateNewItem_WhenCalled_ReturnsAnEmptyGit()
    {
        // Act
        ItemData item = CliMenuTestAccess.InvokeCreateNewItem(CreateSut());

        // Assert
        var git = Assert.IsType<GitDataModel>(item);
        Assert.Null(git.GitProjectAddress);
    }

    //the record menu of a server git has only the commands of the cruder itself (delete, edit)
    [Fact]
    public void FillDetailsSubMenu_WhenCalled_AddsNothing()
    {
        // Arrange
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateSut().FillDetailsSubMenu(detailsMenu, "RepoA");

        // Assert
        Assert.Empty(CliMenuTestAccess.GetMenuItems(detailsMenu));
    }

    private GitStsCruder CreateSut()
    {
        return GitStsCruder.Create(_logger.Object, _httpClientFactory.Object, _memoryCache, _parametersManager.Object);
    }

    private static GitDataModel NewRepoA()
    {
        return new GitDataModel
        {
            GitProjectAddress = RepoAAddress, GitProjectFolderName = "RepoA", GitIgnorePatternName = "CSharp"
        };
    }

    private string TempFolder()
    {
        return _tempFolder ??= Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    }

    private string CreateBareRepository()
    {
        string repository = Path.Combine(TempFolder(), "repository.git");
        using Process git = Process.Start(new ProcessStartInfo("git", $"init --bare \"{repository}\"")
        {
            RedirectStandardOutput = true, RedirectStandardError = true
        })!;
        git.WaitForExit();
        Assert.Equal(0, git.ExitCode);
        return repository;
    }

    private static StsGitDataModel GitRepo(string name, string address)
    {
        return new StsGitDataModel
        {
            GitProjectName = name,
            GitProjectAddress = address,
            GitProjectFolderName = name,
            GitIgnorePatternName = "CSharp"
        };
    }

    private void ServerLists(params StsGitDataModel[] gitRepos)
    {
        _server.Respond(ListRequest, HttpStatusCode.OK, JsonSerializer.Serialize(gitRepos.ToList()));
    }

    //GET gitrepo/{key}: the stored repo for its key in any case, 404 GitWithKeyNotFound for another key
    private void RespondToKeyRequestsLikeServer()
    {
        string repoA = JsonSerializer.Serialize(GitRepo("RepoA", RepoAAddress));
        _server.Respond("GET /api/v1/git/gitrepo/RepoA", HttpStatusCode.OK, repoA);
        _server.Respond("GET /api/v1/git/gitrepo/REPOA", HttpStatusCode.OK, repoA);
        _server.Respond("GET /api/v1/git/gitrepo/RepoNew", HttpStatusCode.NotFound,
            """{"title":"GitWithKeyNotFound","status":404,"detail":"Git With Key RepoNew Not Found"}""");
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
