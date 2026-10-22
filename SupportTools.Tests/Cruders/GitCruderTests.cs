using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliTools.CliMenuCommands;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.FieldEditors;
using SupportTools.Menu.SyncRegistry;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemTools.SystemToolsShared;
using Xunit;

namespace SupportTools.Tests.Cruders;

//the local Gits list (Support Tools Parameters Editor → Gits). Create also reads the server's gits: the status of a
//git names the server's git with the same address
[Collection(ConsoleCaptureCollection.Name)]
public sealed class GitCruderTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ListRequest = "GET /api/v1/git/gitrepos";
    private const string RepoAAddress = "git@github.com:x/a.git";

    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly TextWriter _originalConsoleOutput;
    private readonly SupportToolsParameters _parameters = new();
    private readonly Mock<IParametersManager> _parametersManager = new();
    private readonly StubHttpMessageHandler _server = new();
    private string? _tempFolder;

    public GitCruderTests()
    {
        _parameters.Gits["RepoA"] = new GitDataModel
        {
            GitProjectAddress = RepoAAddress, GitProjectFolderName = "A", GitIgnorePatternName = "CSharp"
        };
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
        if (_tempFolder is not null)
        {
            GitTestRepositories.DeleteFolder(_tempFolder);
        }
    }

    [Fact]
    public void Constructor_WhenCalled_NamesTheGitRecords()
    {
        // Act
        var sut = new GitCruder(_logger.Object, _httpClientFactory.Object, _parametersManager.Object, []);

        // Assert
        Assert.Equal("Git", sut.CrudName);
        Assert.Equal("Gits", sut.CrudNamePlural);
    }

    //New asks for the address, the folder and the gitignore pattern, which is chosen from the local templates
    [Fact]
    public void Constructor_WhenCalled_AsksNewRecordsForAddressFolderAndGitIgnorePattern()
    {
        // Act
        List<FieldEditor> fieldEditors = CliMenuTestAccess.GetFieldEditors(CreateLocalSut());

        // Assert
        Assert.Equal([
            nameof(GitDataModel.GitProjectAddress), nameof(GitDataModel.GitProjectFolderName),
            nameof(GitDataModel.GitIgnorePatternName)
        ], fieldEditors.Where(x => x.EnterFieldDataOnCreate).Select(x => x.PropertyName));
        Assert.Equal(3, fieldEditors.Count);
        Assert.IsType<GitIgnorePathNameFieldEditor>(fieldEditors[2]);
    }

    [Fact]
    public void Create_WhenSupportToolsServerIsNotSpecified_ListsTheLocalGitsWithoutCallingTheServer()
    {
        // Act
        var sut = GitCruder.Create(_logger.Object, _httpClientFactory.Object, _parametersManager.Object);

        // Assert
        Assert.Equal($"{RepoAAddress} Usage count is: 0", sut.GetStatusFor("RepoA"));
        Assert.Contains("supportToolsServerWebApiClientName does not specified", ConsoleText(),
            StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    //the server git with the same address is named in the status, whatever its name is on the server
    [Fact]
    public void Create_WhenServerHasAGitWithTheSameAddress_NamesItInTheStatus()
    {
        // Arrange
        UseServer();
        ServerLists(ServerGit("OtherRepo", "git@github.com:x/other.git"), ServerGit("ServerRepoA", RepoAAddress));

        // Act
        var sut = GitCruder.Create(_logger.Object, _httpClientFactory.Object, _parametersManager.Object);

        // Assert
        Assert.Equal($"{RepoAAddress} Usage count is: 0- rem name is: ServerRepoA", sut.GetStatusFor("RepoA"));
        Assert.Equal([ListRequest], _server.Requests);
    }

    [Fact]
    public void Create_WhenServerHasNoGitWithTheAddress_ShowsOnlyTheUsage()
    {
        // Arrange
        UseServer();
        ServerLists(ServerGit("OtherRepo", "git@github.com:x/other.git"));

        // Act
        var sut = GitCruder.Create(_logger.Object, _httpClientFactory.Object, _parametersManager.Object);

        // Assert
        Assert.Equal($"{RepoAAddress} Usage count is: 0", sut.GetStatusFor("RepoA"));
    }

    [Fact]
    public void Create_WhenServerListFails_ReportsItAndShowsNoServerNames()
    {
        // Arrange
        UseServer();
        _server.Respond(ListRequest, HttpStatusCode.BadRequest,
            """{"title":"SomeError","status":400,"detail":"Server list is broken"}""");

        // Act
        var sut = GitCruder.Create(_logger.Object, _httpClientFactory.Object, _parametersManager.Object);

        // Assert
        string console = ConsoleText();
        Assert.Contains("could not received remoteGits", console, StringComparison.Ordinal);
        Assert.Contains("Server list is broken", console, StringComparison.Ordinal);
        Assert.Equal($"{RepoAAddress} Usage count is: 0", sut.GetStatusFor("RepoA"));
    }

    //a wrong ApiClient name throws while the client is created: the local list still opens
    [Fact]
    public void Create_WhenNamedApiClientDoesNotExist_WritesTheExceptionAndStillListsTheLocalGits()
    {
        // Arrange
        _parameters.SupportToolsServerWebApiClientName = ApiClientName;

        // Act
        var sut = GitCruder.Create(_logger.Object, _httpClientFactory.Object, _parametersManager.Object);

        // Assert
        Assert.Contains($"ApiClient with name {ApiClientName} does not exists", ConsoleText(),
            StringComparison.Ordinal);
        Assert.True(sut.ContainsRecordWithKey("RepoA"));
        Assert.Empty(_server.Requests);
    }

    //the commands of the whole list come right after New, before the gits
    [Fact]
    public void GetListMenu_WhenCalled_OffersTheGitCommandsBeforeTheGits()
    {
        // Arrange
        _parameters.Gits["RepoB"] = new GitDataModel { GitProjectAddress = "git@github.com:x/b.git" };

        // Act
        CliMenuSet listMenu = CreateLocalSut().GetListMenu();

        // Assert
        Assert.Equal("Gits", listMenu.Caption);
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(listMenu).Select(x => x.CliMenuCommand)];
        Assert.IsType<NewItemCliMenuCommand>(commands[0]);
        Assert.IsType<UpdateGitProjectsCliMenuCommand>(commands[1]);
        Assert.IsType<UpdateGitProjectsFromServerCliMenuCommand>(commands[2]);
        Assert.Equal("Sync Gits With SupportToolsServer...",
            Assert.IsType<SyncRegistryCliMenuCommand>(commands[3]).Name);
        Assert.IsType<FindGitProjectsWithoutMatchingProjectsCliMenuCommand>(commands[4]);
        Assert.Equal(["RepoA", "RepoB"], commands.OfType<ItemSubMenuCliMenuCommand>().Select(x => x.Name));
        Assert.Equal(8, commands.Count);
    }

    //the dictionary of the parameters is the list, not the dictionary given to the constructor
    [Fact]
    public void GetCrudersDictionary_WhenCalled_ReturnsTheGitsOfTheParameters()
    {
        // Act
        Dictionary<string, ItemData> result = CliMenuTestAccess.GetCrudersDictionary(CreateLocalSut());

        // Assert
        Assert.Same(_parameters.Gits["RepoA"], Assert.Single(result).Value);
    }

    [Theory]
    [InlineData("RepoA", true)]
    [InlineData("REPOA", false)]
    [InlineData("RepoB", false)]
    public void ContainsRecordWithKey_WhenCalled_LooksTheKeyUpInTheParameters(string recordKey, bool expected)
    {
        // Act
        bool result = CreateLocalSut().ContainsRecordWithKey(recordKey);

        // Assert
        Assert.Equal(expected, result);
    }

    //the cruder only changes the parameters: the menu saves them afterwards
    [Fact]
    public async Task UpdateRecordWithKey_WhenRecordIsAGit_ReplacesItWithoutSaving()
    {
        // Arrange
        var newGit = new GitDataModel { GitProjectAddress = "git@github.com:x/new.git" };

        // Act
        await CreateLocalSut().UpdateRecordWithKey("RepoA", newGit);

        // Assert
        Assert.Same(newGit, _parameters.Gits["RepoA"]);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task UpdateRecordWithKey_WhenRecordIsNotAGit_ThrowsAndKeepsTheGit()
    {
        // Arrange
        GitDataModel repoA = _parameters.Gits["RepoA"];
        GitCruder sut = CreateLocalSut();

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await sut.UpdateRecordWithKey("RepoA", new TextItemData()));

        // Assert
        Assert.Equal("newGit is null in GitCruder.UpdateRecordWithKey", exception.Message);
        Assert.Same(repoA, _parameters.Gits["RepoA"]);
    }

    [Fact]
    public async Task AddRecordWithKey_WhenRecordIsAGit_AddsItWithoutSaving()
    {
        // Arrange
        var newGit = new GitDataModel { GitProjectAddress = "git@github.com:x/b.git" };

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(CreateLocalSut(), "RepoB", newGit);

        // Assert
        Assert.Same(newGit, _parameters.Gits["RepoB"]);
        Assert.Equal(2, _parameters.Gits.Count);
        VerifyNothingSaved();
    }

    //AddRecordWithKey is reached through reflection, which wraps the exception
    [Fact]
    public async Task AddRecordWithKey_WhenRecordIsNotAGit_ThrowsAndAddsNothing()
    {
        // Act
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(() =>
            CliMenuTestAccess.InvokeAddRecordWithKey(CreateLocalSut(), "RepoB", new TextItemData()));

        // Assert
        Assert.Equal("newGit is null in GitCruder.AddRecordWithKey", exception.InnerException?.Message);
        Assert.False(_parameters.Gits.ContainsKey("RepoB"));
    }

    [Fact]
    public async Task RemoveRecordWithKey_WhenCalled_RemovesTheGitWithoutSaving()
    {
        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(CreateLocalSut(), "RepoA");

        // Assert
        Assert.Empty(_parameters.Gits);
        VerifyNothingSaved();
    }

    [Fact]
    public void CheckValidation_WhenItemIsNotAGit_ReportsItAndReturnsFalse()
    {
        // Act
        bool result = CreateLocalSut().CheckValidation(new TextItemData());

        // Assert
        Assert.False(result);
        Assert.Contains("item is not GitDataModel in GitCruder.CheckValidation", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CheckValidation_WhenAddressIsMissing_ReportsItAndReturnsFalse()
    {
        // Act
        bool result = CreateLocalSut().CheckValidation(new GitDataModel());

        // Assert
        Assert.False(result);
        Assert.Contains("gitDataModel.GitProjectAddress is null in GitCruder.CheckValidation", ConsoleText(),
            StringComparison.Ordinal);
    }

    //the address is checked with git ls-remote on the console; a local repository answers without network
    [Fact]
    public void CheckValidation_WhenAddressIsARepository_ReturnsTrue()
    {
        // Arrange
        string repository = Path.Combine(TempFolder(), "repository.git");
        GitTestRepositories.RunGit("init", "--bare", repository);

        // Act
        bool result = CreateLocalSut().CheckValidation(new GitDataModel { GitProjectAddress = repository });

        // Assert
        Assert.True(result);
        Assert.Contains($"ls-remote -- {repository}", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public void CheckValidation_WhenAddressIsNotARepository_ReturnsFalse()
    {
        // Arrange
        string missingRepository = Path.Combine(TempFolder(), "missing.git");

        // Act
        bool result = CreateLocalSut().CheckValidation(new GitDataModel { GitProjectAddress = missingRepository });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckValidation_WhenGitCannotBeStarted_LogsTheErrorAndReturnsFalse()
    {
        // Arrange
        _parameters.GitExecutablePath = Path.Combine(TempFolder(), "missing-git.exe");

        // Act
        bool result = CreateLocalSut().CheckValidation(new GitDataModel { GitProjectAddress = RepoAAddress });

        // Assert
        Assert.False(result);
        _logger.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == "Error occurred during git validation"),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void GetStatusFor_WhenGitDoesNotExist_ReportsIt()
    {
        // Act
        string status = CreateLocalSut().GetStatusFor("RepoB");

        // Assert
        Assert.Equal("ERROR: Git address Not found", status);
    }

    //a project counts once per list that holds the git; other gits and other spellings do not count
    [Fact]
    public void GetStatusFor_WhenProjectsUseTheGit_CountsBothKindsOfUsage()
    {
        // Arrange
        _parameters.Projects["Main"] = new ProjectModel { GitProjectNames = ["RepoA"] };
        _parameters.Projects["Seeder"] = new ProjectModel { ScaffoldSeederGitProjectNames = ["RepoA"] };
        _parameters.Projects["Both"] = new ProjectModel
        {
            GitProjectNames = ["RepoA"], ScaffoldSeederGitProjectNames = ["RepoA"]
        };
        _parameters.Projects["Other"] = new ProjectModel
        {
            GitProjectNames = ["RepoB", "REPOA"], ScaffoldSeederGitProjectNames = ["RepoB"]
        };

        // Act
        string status = CreateLocalSut().GetStatusFor("RepoA");

        // Assert
        Assert.Equal($"{RepoAAddress} Usage count is: 4", status);
    }

    [Fact]
    public void CreateNewItem_WhenCalled_ReturnsAnEmptyGit()
    {
        // Act
        ItemData item = CliMenuTestAccess.InvokeCreateNewItem(CreateLocalSut());

        // Assert
        var git = Assert.IsType<GitDataModel>(item);
        Assert.Null(git.GitProjectAddress);
        Assert.Null(git.GitProjectFolderName);
        Assert.Null(git.GitIgnorePatternName);
    }

    //after the editors of the record: the update of its git projects and a link to every project that uses the git as
    //a main git, in name order
    [Fact]
    public void FillDetailsSubMenu_WhenProjectsUseTheGit_AddsTheUpdateCommandAndLinksToTheProjects()
    {
        // Arrange
        _parameters.Projects["ProjectB"] =
            new ProjectModel { ProjectGroupName = "Group1", GitProjectNames = ["RepoA"] };
        _parameters.Projects["ProjectA"] =
            new ProjectModel { ProjectGroupName = "Group2", GitProjectNames = ["RepoA"] };
        _parameters.Projects["SeederOnly"] = new ProjectModel { ScaffoldSeederGitProjectNames = ["RepoA"] };
        _parameters.Projects["Other"] = new ProjectModel { GitProjectNames = ["RepoB"] };
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateLocalSut().FillDetailsSubMenu(detailsMenu, "RepoA");

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(detailsMenu).Select(x => x.CliMenuCommand)];
        Assert.IsType<RecordKeyEditorCliMenuCommand>(commands[0]);
        Assert.IsType<UpdateGitProjectCliMenuCommand>(commands[4]);
        List<InfoCliMenuCommand> projectLinks = [.. commands.OfType<InfoCliMenuCommand>()];
        Assert.Equal(["ProjectA", "ProjectB"], projectLinks.Select(x => x.Name));
        Assert.Equal(["Group2/ProjectA", "Group1/ProjectB"], projectLinks.Select(x => x.GetMenuLinkToGo()));
        Assert.Equal(7, commands.Count);
    }

    [Fact]
    public void FillDetailsSubMenu_WhenNoProjectUsesTheGit_AddsOnlyTheEditorsAndTheUpdateCommand()
    {
        // Arrange
        var detailsMenu = new CliMenuSet("Details");

        // Act
        CreateLocalSut().FillDetailsSubMenu(detailsMenu, "RepoA");

        // Assert
        List<CliMenuCommand> commands = [.. CliMenuTestAccess.GetMenuItems(detailsMenu).Select(x => x.CliMenuCommand)];
        Assert.Equal(5, commands.Count);
        Assert.IsType<UpdateGitProjectCliMenuCommand>(commands[4]);
    }

    private GitCruder CreateLocalSut()
    {
        return new GitCruder(_logger.Object, _httpClientFactory.Object, _parametersManager.Object, []);
    }

    private void UseServer()
    {
        _parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "http://127.0.0.1:0/api/v1" };
    }

    private void ServerLists(params StsGitDataModel[] gitRepos)
    {
        _server.Respond(ListRequest, HttpStatusCode.OK, JsonSerializer.Serialize(gitRepos.ToList()));
    }

    private static StsGitDataModel ServerGit(string name, string address)
    {
        return new StsGitDataModel
        {
            GitProjectName = name,
            GitProjectAddress = address,
            GitProjectFolderName = name,
            GitIgnorePatternName = "CSharp"
        };
    }

    private void VerifyNothingSaved()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    private string TempFolder()
    {
        return _tempFolder ??= Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    //remembers every request ("METHOD path") and answers by "METHOD path" (200 without a body by default)
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _responses = [];

        public List<string> Requests { get; } = [];

        public void Respond(string request, HttpStatusCode statusCode, string body)
        {
            _responses[request] = (statusCode, body);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string key = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add(key);

            if (!_responses.TryGetValue(key, out (HttpStatusCode StatusCode, string Body) response))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request });
            }

            return Task.FromResult(new HttpResponseMessage(response.StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(response.Body, Encoding.UTF8,
                    response.StatusCode == HttpStatusCode.OK ? "application/json" : "application/problem+json")
            });
        }
    }
}
