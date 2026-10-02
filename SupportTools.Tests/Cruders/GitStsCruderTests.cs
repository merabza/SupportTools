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
        _parametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
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

    //Delete is RemoveRecordWithKey followed by Save: the local git goes too, if no project uses it
    [Fact]
    public async Task RemoveThenSave_WhenLocalGitIsNotUsed_RemovesItLocallyAndSavesTheParameters()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel { GitProjectAddress = RepoAAddress };
        _parameters.Gits["RepoB"] = new GitDataModel();
        _parameters.Projects["Other"] = new ProjectModel { GitProjectNames = ["RepoB"] };
        GitStsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        bool result = await sut.Save("GitFromServer with Name RepoA deleted successfully.");

        // Assert
        Assert.True(result);
        Assert.Equal(["RepoB"], _parameters.Gits.Keys);
        VerifyParametersSavedOnce("Local git RepoA removed");
    }

    //the server matches the names ignoring case, so does the local lookup
    [Fact]
    public async Task RemoveThenSave_WhenLocalNameDiffersInCase_RemovesTheLocalGit()
    {
        // Arrange
        _parameters.Gits["repoa"] = new GitDataModel();
        GitStsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        await sut.Save("Deleted");

        // Assert
        Assert.Empty(_parameters.Gits);
        VerifyParametersSavedOnce("Local git repoa removed");
    }

    //projects would refer to a missing git: such a local git stays
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemoveThenSave_WhenAProjectUsesTheLocalGit_KeepsItAndSavesNothing(bool asScaffoldSeederGit)
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel { GitProjectAddress = RepoAAddress };
        _parameters.Projects["Project"] = asScaffoldSeederGit
            ? new ProjectModel { ScaffoldSeederGitProjectNames = ["repoa"] }
            : new ProjectModel { GitProjectNames = ["RepoA"] };
        GitStsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        bool result = await sut.Save("Deleted");

        // Assert
        Assert.True(result);
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        Assert.Contains("Local git RepoA is used by projects Project and was not removed", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyNothingSaved();
    }

    //the warning names every project that uses the git, in the order of their names
    [Fact]
    public async Task RemoveThenSave_WhenSeveralProjectsUseTheLocalGit_NamesThemAll()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel();
        _parameters.Projects["Zeta"] = new ProjectModel { GitProjectNames = ["RepoA"] };
        _parameters.Projects["Middle"] = new ProjectModel { GitProjectNames = ["Other"] };
        _parameters.Projects["Alpha"] = new ProjectModel { ScaffoldSeederGitProjectNames = ["repoa"] };
        GitStsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        await sut.Save("Deleted");

        // Assert
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        VerifyWarningLogged("Local git RepoA is used by projects Alpha, Zeta and was not removed");
        VerifyNothingSaved();
    }

    [Fact]
    public async Task RemoveThenSave_WhenServerRefuses_KeepsTheLocalGit()
    {
        // Arrange
        _server.Respond("DELETE /api/v1/git/deletegitrepo/RepoA", HttpStatusCode.NotFound,
            """{"title":"GitWithKeyNotFound","status":404,"detail":"Git With Key RepoA Not Found"}""");
        _parameters.Gits["RepoA"] = new GitDataModel();
        GitStsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        await sut.Save("Deleted");

        // Assert
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task RemoveThenSave_WhenThereIsNoLocalGit_SavesNothing()
    {
        // Arrange
        _parameters.Gits["RepoB"] = new GitDataModel();
        GitStsCruder sut = CreateSut();

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        bool result = await sut.Save("Deleted");

        // Assert
        Assert.True(result);
        Assert.Equal(["RepoB"], _parameters.Gits.Keys);
        VerifyNothingSaved();
    }

    //Save also ends the other operations (an edit, a new record): only a deletion from the server reaches local gits
    [Fact]
    public async Task Save_WhenNothingWasRemovedFromServer_LeavesLocalGitsAlone()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel();
        GitStsCruder sut = CreateSut();
        await sut.UpdateRecordWithKey("RepoA", NewRepoA());

        // Act
        bool result = await sut.Save("Updated");

        // Assert
        Assert.True(result);
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        VerifyNothingSaved();
    }

    //renaming is deleting and adding: the local git gets the new name, and so do the references to it
    [Fact]
    public async Task ChangeRecordKey_WhenLocalGitExists_RenamesItWithItsReferencesAndSavesOnce()
    {
        // Arrange
        var localGit = new GitDataModel { GitProjectAddress = RepoAAddress };
        _parameters.Gits["RepoA"] = localGit;
        _parameters.Gits["Other"] = new GitDataModel();
        _parameters.Projects["Direct"] = new ProjectModel { GitProjectNames = ["Other", "RepoA"] };
        _parameters.Projects["Seeder"] = new ProjectModel { ScaffoldSeederGitProjectNames = ["repoa"] };
        _parameters.GitProjects["App"] = new GitProjectDataModel { GitName = "RepoA" };
        _parameters.GitProjects["OtherApp"] = new GitProjectDataModel { GitName = "Other" };

        // Act
        bool result = await CreateSut().ChangeRecordKey("RepoA", "RepoB");

        // Assert
        Assert.True(result);
        Assert.Equal(["Other", "RepoB"], _parameters.Gits.Keys.Order());
        Assert.Same(localGit, _parameters.Gits["RepoB"]);
        Assert.Equal(["Other", "RepoB"], _parameters.Projects["Direct"].GitProjectNames);
        Assert.Equal(["RepoB"], _parameters.Projects["Seeder"].ScaffoldSeederGitProjectNames);
        Assert.Equal("RepoB", _parameters.GitProjects["App"].GitName);
        Assert.Equal("Other", _parameters.GitProjects["OtherApp"].GitName);
        VerifyParametersSavedOnce("Local git RepoA renamed to RepoB");
    }

    [Fact]
    public async Task ChangeRecordKey_WhenNewNameIsTakenLocally_LeavesLocalGitsAlone()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel();
        _parameters.Gits["RepoB"] = new GitDataModel();
        _parameters.Projects["Direct"] = new ProjectModel { GitProjectNames = ["RepoA"] };

        // Act
        bool result = await CreateSut().ChangeRecordKey("RepoA", "RepoB");

        // Assert
        Assert.True(result);
        Assert.Equal(["RepoA", "RepoB"], _parameters.Gits.Keys.Order());
        Assert.Equal(["RepoA"], _parameters.Projects["Direct"].GitProjectNames);
        Assert.Contains("Local git RepoB already exists, local git RepoA was not renamed", _consoleOutput.ToString(),
            StringComparison.Ordinal);
        VerifyNothingSaved();
    }

    //the add half of a rename sends nothing for an incomplete record: the local git keeps its name
    [Theory]
    [InlineData(nameof(TextItemData))]
    [InlineData(nameof(GitDataModel.GitIgnorePatternName))]
    [InlineData(nameof(GitDataModel.GitProjectAddress))]
    [InlineData(nameof(GitDataModel.GitProjectFolderName))]
    public async Task RemoveThenAdd_WhenNewRecordIsIncomplete_LeavesTheLocalGitAlone(string missingPart)
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel();
        GitStsCruder sut = CreateSut();
        ItemData newRecord = missingPart switch
        {
            nameof(GitDataModel.GitIgnorePatternName) => new GitDataModel
            {
                GitProjectAddress = RepoAAddress, GitProjectFolderName = "RepoA"
            },
            nameof(GitDataModel.GitProjectAddress) => new GitDataModel
            {
                GitProjectFolderName = "RepoA", GitIgnorePatternName = "CSharp"
            },
            nameof(GitDataModel.GitProjectFolderName) => new GitDataModel
            {
                GitProjectAddress = RepoAAddress, GitIgnorePatternName = "CSharp"
            },
            _ => new TextItemData()
        };

        // Act
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "RepoB", newRecord);
        await sut.Save("Updated");

        // Assert
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        Assert.DoesNotContain(_server.Requests, x => x.Request.StartsWith("POST", StringComparison.Ordinal));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task RemoveThenAdd_WhenServerIsNoLongerSpecified_LeavesTheLocalGitAlone()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel();
        GitStsCruder sut = CreateSut();
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        _parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "RepoB", NewRepoA());
        await sut.Save("Updated");

        // Assert
        Assert.Contains("supportToolsServerApiClient is null", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        VerifyNothingSaved();
    }

    //a request that throws is not an accepted record
    [Fact]
    public async Task RemoveThenAdd_WhenAddingThrows_LeavesTheLocalGitAlone()
    {
        // Arrange
        _parameters.Gits["RepoA"] = new GitDataModel();
        GitStsCruder sut = CreateSut();
        await CliMenuTestAccess.InvokeRemoveRecordWithKey(sut, "RepoA");
        _parameters.ApiClients[ApiClientName] = new ApiClientSettings { Server = "not a server address" };

        // Act
        await CliMenuTestAccess.InvokeAddRecordWithKey(sut, "RepoB", NewRepoA());
        await sut.Save("Updated");

        // Assert
        Assert.Contains("Invalid URI", _consoleOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        VerifyNothingSaved();
    }

    //the server deleted the old key but refused the new one: the local git keeps its name
    [Fact]
    public async Task ChangeRecordKey_WhenServerRefusesTheNewKey_LeavesTheLocalGitAlone()
    {
        // Arrange
        _server.Respond("POST /api/v1/git/updategitrepo/RepoB", HttpStatusCode.Conflict,
            """{"title":"GitAddressIsInUse","status":409,"detail":"Git Address Is Used By RepoC"}""");
        _parameters.Gits["RepoA"] = new GitDataModel();
        GitStsCruder sut = CreateSut();

        // Act
        await sut.ChangeRecordKey("RepoA", "RepoB");
        await sut.Save("Updated");

        // Assert
        Assert.Equal(["RepoA"], _parameters.Gits.Keys);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task ChangeRecordKey_WhenThereIsNoLocalGit_ChangesOnlyTheServer()
    {
        // Act
        bool result = await CreateSut().ChangeRecordKey("RepoA", "RepoB");

        // Assert
        Assert.True(result);
        Assert.Empty(_parameters.Gits);
        VerifyNothingSaved();
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
        Assert.DoesNotContain("NullReferenceException", _consoleOutput.ToString(), StringComparison.Ordinal);
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
        Assert.Contains($"ls-remote -- {repository}", _consoleOutput.ToString(), StringComparison.Ordinal);
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

    //local changes are saved as the whole parameters object
    private void VerifyParametersSavedOnce(string message)
    {
        _parametersManager.Verify(x => x.Save(_parameters, message, null, It.IsAny<CancellationToken>()), Times.Once);
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyWarningLogged(string message)
    {
        _logger.Verify(
            x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == message), It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private void VerifyNothingSaved()
    {
        _parametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
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
