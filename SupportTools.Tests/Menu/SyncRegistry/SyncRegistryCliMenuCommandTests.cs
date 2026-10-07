using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData.Models;
using LibSupportToolsServerWork.Registry.Adapters;
using LibSupportToolsServerWork.Registry.Paths;
using LibSupportToolsServerWork.Registry.Sync;
using Microsoft.Extensions.Logging;
using Moq;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SyncRegistry;
using SupportTools.Tests.Registry.Adapters;
using SupportTools.Tests.Registry.Mappers;
using SupportTools.Tests.Registry.Sync;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Menu.SyncRegistry;

//the command over fake adapters (FakeRegistrySyncAdapter) and the real engine: the adapters keep both sides in memory
//and log every server and local operation, so a choice is checked by what it does. Only the connection check goes to
//the fake server (FakeSupportToolsServer). The menu answers are the names of the items to choose, in order
[Collection(ConsoleCaptureCollection.Name)]
public sealed class SyncRegistryCliMenuCommandTests : IDisposable
{
    private const string ApiClientName = "SupportToolsServer";
    private const string ApiKey = "fake-api-key-of-the-tests";
    private const string Environments = "Environments";
    private const string Servers = "Servers";
    private const string EnvironmentsRequest = "GET /api/v1/environments";

    //an answer that chooses no menu item (-1), like the "-" key of a menu
    private const string NoAnswer = "<none>";

    //an answer one past the last menu item
    private const string OutOfRangeAnswer = "<out of range>";

    private readonly List<FakeRegistrySyncAdapter> _adapters = [];
    private readonly Queue<string> _answers = new();
    private readonly StringWriter _consoleOutput = new(CultureInfo.InvariantCulture);
    private readonly FakeRegistrySyncAdapter _environments;
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly TextWriter _originalConsoleOutput;
    //the field names of the menu questions, in order
    private readonly List<string> _questions = [];

    private readonly FakeSupportToolsServer _server = new();
    private readonly List<CliMenuSet> _shownMenus = [];
    private readonly RegistrySyncTestContext _sync = new();

    //runs before the answer is given: another computer that changes the server while the user decides
    private Action<string>? _beforeAnswer;

    public SyncRegistryCliMenuCommandTests()
    {
        _sync.Parameters.SupportToolsServerWebApiClientName = ApiClientName;
        _sync.Parameters.ApiClients[ApiClientName] =
            new ApiClientSettings { Server = FakeSupportToolsServer.Address, ApiKey = ApiKey };
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(_server, false));
        _environments = AddAdapter(Environments, 10);

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
        var sut = new SyncRegistryCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _sync.ParametersManager.Object);

        // Assert
        Assert.Equal("Sync Registry With SupportToolsServer...", sut.Name);
        Assert.Equal(EMenuAction.Reload, sut.MenuActionOnBodySuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task RunBody_WhenApiClientNameIsNotSet_ReportsItWithoutCallingTheServer(string? apiClientName)
    {
        // Arrange
        _sync.Parameters.SupportToolsServerWebApiClientName = apiClientName;

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        Assert.Contains("SupportToolsServerWebApiClientName is not set: choose the ApiClient of SupportToolsServer " +
                        "in Support Tools Parameters Editor", ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task RunBody_WhenApiClientDoesNotExist_ReportsItsNameWithoutCallingTheServer()
    {
        // Arrange
        _sync.Parameters.SupportToolsServerWebApiClientName = "MissingClient";

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        Assert.Contains("ApiClient MissingClient (SupportToolsServerWebApiClientName) does not exist", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    [Fact]
    public async Task RunBody_WhenServerAddressIsNotSet_ReportsItWithoutCallingTheServer()
    {
        // Arrange
        _sync.Parameters.ApiClients[ApiClientName].Server = null;

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        Assert.Contains("The server address of ApiClient SupportToolsServer is not set", ConsoleText(),
            StringComparison.Ordinal);
        Assert.Empty(_server.Requests);
    }

    //the API key check of the server answers 401 without a body; a ProblemDetails 401 has the code Unauthorized
    [Theory]
    [InlineData("")]
    [InlineData("Unauthorized")]
    public async Task RunBody_WhenServerRefusesTheApiKey_NamesTheApiClientAndItsAddressAndAsksToCheckTheApiKey(
        string code)
    {
        // Arrange
        _server.Fail(EnvironmentsRequest, HttpStatusCode.Unauthorized, code);

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        string console = ConsoleText();
        Assert.Contains("Cannot connect to SupportToolsServer (ApiClient SupportToolsServer, address " +
                        $"{FakeSupportToolsServer.Address})", console, StringComparison.Ordinal);
        Assert.Contains("check the ApiKey of ApiClient SupportToolsServer", console, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, console, StringComparison.Ordinal);
        //the API client is created without the console, so only the command reports the error
        Assert.DoesNotContain("answer after uri", console, StringComparison.Ordinal);
        Assert.Empty(_shownMenus);
    }

    [Fact]
    public async Task RunBody_WhenServerDoesNotAnswer_NamesTheAddressAndTheTransportError()
    {
        // Arrange
        _server.IsUnavailable = true;

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        string console = ConsoleText();
        Assert.Contains($"address {FakeSupportToolsServer.Address}): Api request failed:", console,
            StringComparison.Ordinal);
        Assert.Contains("The server does not answer", console, StringComparison.Ordinal);
        Assert.DoesNotContain("check the ApiKey", console, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiKey, console, StringComparison.Ordinal);
        Assert.DoesNotContain("request to ", console, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenLocalKeysDifferOnlyByCase_RefusesWithTheirReportBeforePlanning()
    {
        // Arrange
        _sync.Parameters.Gits["AppA"] = new GitDataModel();
        _sync.Parameters.Gits["appa"] = new GitDataModel();
        _environments.Local["Dev"] = "d";

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        string console = ConsoleText();
        Assert.Contains("These local keys differ only by case, so they cannot be matched with the server records. " +
                        "Rename or remove the extra records, then sync again:", console, StringComparison.Ordinal);
        Assert.Contains("  Gits: AppA / appa", console, StringComparison.Ordinal);
        Assert.DoesNotContain("Registry sync plan", console, StringComparison.Ordinal);
        Assert.Empty(_shownMenus);
        Assert.Empty(_sync.Calls);
    }

    [Fact]
    public async Task RunBody_WhenPlanCannotBeCreated_ReportsTheError()
    {
        // Arrange
        _environments.ServerRecordsError = ApiClientErrors.ApiRequestFailed("http://sts/api: connection refused");

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        Assert.Contains(
            "The registry sync plan was not created: Api request failed: http://sts/api: connection refused",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Empty(_shownMenus);
    }

    //the first sync finds equal records: nothing to ask, but their state is recorded and saved
    [Fact]
    public async Task RunBody_WhenEverythingIsInSync_RecordsTheStateWithoutAsking()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 7);

        // Act
        bool result = await Run();

        // Assert
        Assert.True(result);
        Assert.Empty(_shownMenus);
        Assert.Empty(_sync.Calls);
        Assert.Equal(7, _sync.State.Collections[Environments].Records["Dev"].Version);
        _sync.VerifySaved(Times.Once());
        Assert.Contains("The local registry and SupportToolsServer are in sync", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenRecordsDiffer_ShowsTheSummaryAndEveryChoiceWithItsOperations()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);
        _environments.Local["Prod"] = "mine";
        _environments.Server["Prod"] = new FakeContract("Prod", "theirs", 2);
        FakeRegistrySyncAdapter servers = AddAdapter(Servers, 100);
        _sync.AddSyncedRecord(servers, "PAZISI", "p", 1);

        // Act
        bool result = await Run("Cancel");

        // Assert
        Assert.True(result);
        Assert.Equal([
            "Registry sync plan:",
            "Collection                  Pull  Push  Conflict  InSync  Skipped",
            "Environments                   1     1         1       0        0",
            "Servers                        0     0         0       1        0",
            "Total                          1     1         1       1        0"
        ], ConsoleLines().Take(5));
        Assert.Equal([
            "Apply (send 1, change here 1)",
            "Pull only (send 0, change here 1)",
            "Push only (send 1, change here 0)",
            "Resolve conflicts (1 conflicts, 0 resolved)",
            "Local wins for first sync conflicts (1 conflicts)",
            "Show details (the records to sync)",
            "Dry run (what Apply sends, nothing is sent)",
            "Exclude record from sync (on this computer)",
            "Cancel (nothing changes)"
        ], MenuLines(Assert.Single(_shownMenus)));
        Assert.Empty(_sync.Calls);
        _sync.VerifySaved(Times.Never());
        Assert.Contains("Canceled, nothing was synced", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenApplySelected_SendsAndTakesTheChangesWithProgressAndReport()
    {
        // Arrange
        _sync.ParametersManager.SetupGet(x => x.ParametersFileName).Returns(@"D:\Fake\SupportTools.json");
        _environments.Local["Dev"] = "d";
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);

        // Act
        bool result = await Run("Apply");

        // Assert
        Assert.True(result);
        Assert.Equal(["Upsert Environments/Dev/0", "ApplyLocal Environments/Test"], _sync.Calls);
        List<string> lines = ConsoleLines();
        Assert.Contains("Sending 1 records to SupportToolsServer...", lines);
        Assert.Contains("1/1 add Environments/Dev: done", lines);
        Assert.Contains("Sync result: done 2", lines);
        Assert.Contains(@"The parameters file before the sync is kept as D:\Fake\SupportTools.json.<date>.bak", lines);
        Assert.DoesNotContain("[ERROR]", ConsoleText(), StringComparison.Ordinal);
        _sync.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task RunBody_WhenPullOnlySelected_TakesTheServerChangesAndSendsNothing()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);

        // Act
        bool result = await Run("Pull only");

        // Assert
        Assert.True(result);
        Assert.Equal(["ApplyLocal Environments/Test"], _sync.Calls);
        Assert.Contains("Sync result: done 1, not selected 1", ConsoleLines());
        Assert.DoesNotContain("Sending", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenPushOnlySelected_SendsTheLocalChangesAndChangesNothingHere()
    {
        // Arrange
        _sync.ParametersManager.SetupGet(x => x.ParametersFileName).Returns(@"D:\Fake\SupportTools.json");
        _environments.Local["Dev"] = "d";
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);

        // Act
        bool result = await Run("Push only");

        // Assert
        Assert.True(result);
        Assert.Equal(["Upsert Environments/Dev/0"], _sync.Calls);
        Assert.False(_environments.Local.ContainsKey("Test"));
        Assert.Contains("Sync result: done 1, not selected 1", ConsoleLines());
        Assert.DoesNotContain(".bak", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenConflictsAreResolved_ApplyExecutesEveryChosenSideAndKeepsTheSkippedOne()
    {
        // Arrange
        ArrangeBothChanged("A");
        ArrangeBothChanged("B");
        ArrangeBothChanged("C");

        // Act
        bool result = await Run("Resolve conflicts", "Local", "Server", "Skip", "Apply");

        // Assert
        Assert.True(result);
        Assert.Equal(["Upsert Environments/A/4", "ApplyLocal Environments/B"], _sync.Calls);
        Assert.Equal([
            "action", "resolution of Environments/A", "resolution of Environments/B", "resolution of Environments/C",
            "action"
        ], _questions);
        Assert.Equal("theirs", _environments.Local["B"]);
        Assert.Equal("mine", _environments.Local["C"]);
        Assert.Contains("Conflict 1/3: Environments/A - changed here and on the server", ConsoleLines());
        Assert.Contains("Resolve conflicts (3 conflicts, 2 resolved)", MenuLines(_shownMenus[^1]));
        Assert.Contains("Apply (send 1, change here 1)", MenuLines(_shownMenus[^1]));
    }

    //a second pass shows the earlier choice and can change it
    [Fact]
    public async Task RunBody_WhenConflictIsResolvedAgain_ShowsTheEarlierChoiceAndKeepsTheNewOne()
    {
        // Arrange
        ArrangeBothChanged("Dev");

        // Act
        bool result = await Run("Resolve conflicts", "Local", "Resolve conflicts", "Server", "Apply");

        // Assert
        Assert.True(result);
        Assert.Contains("Conflict 1/1: Environments/Dev - changed here and on the server, current choice: Local",
            ConsoleLines());
        Assert.Equal(["ApplyLocal Environments/Dev"], _sync.Calls);
    }

    [Fact]
    public async Task RunBody_WhenDifferencesAreShown_WritesTheFieldsOfBothSidesAndAsksAgain()
    {
        // Arrange
        ArrangeBothChanged("Dev");

        // Act
        bool result = await Run("Resolve conflicts", "Show differences", "Skip", "Cancel");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        int index = lines.IndexOf("Differences of Environments/Dev (secrets are hidden):");
        Assert.True(index >= 0);
        Assert.Equal(["  Value", "    local:  mine", "    server: theirs"], lines.Skip(index + 1).Take(3));
        Assert.Empty(_sync.Calls);
    }

    //seed: the server already has some records that differ (for example Gits of the old commands)
    [Fact]
    public async Task RunBody_WhenLocalWinsForFirstSyncConflicts_ApplySendsOnlyThoseConflicts()
    {
        // Arrange
        _environments.Local["Dev"] = "mine";
        _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        ArrangeBothChanged("Prod");

        // Act
        bool result = await Run("Local wins for first sync conflicts", "Apply");

        // Assert
        Assert.True(result);
        Assert.Equal(["Upsert Environments/Dev/4"], _sync.Calls);
        Assert.Contains("1 first sync conflicts are resolved with the local records: Apply or Push only sends them",
            ConsoleLines());
        Assert.Equal("theirs", _environments.Server["Prod"].Value);
    }

    //Pull only takes the conflicts resolved for the server, Push only sends those resolved for this computer
    [Theory]
    [InlineData("Local", "Push only", "Upsert Environments/Dev/4")]
    [InlineData("Server", "Pull only", "ApplyLocal Environments/Dev")]
    [InlineData("Local", "Pull only", null)]
    [InlineData("Server", "Push only", null)]
    public async Task RunBody_WhenOneDirectionIsSelected_ExecutesOnlyTheConflictsResolvedInThatDirection(
        string resolution, string action, string? expectedCall)
    {
        // Arrange
        ArrangeBothChanged("Dev");
        List<string> expectedCalls = expectedCall is null ? [] : [expectedCall];

        // Act
        bool result = await Run("Resolve conflicts", resolution, action);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedCalls, _sync.Calls);
    }

    [Fact]
    public async Task RunBody_WhenDetailsAreShown_ListsTheRecordsByActionWithChangedFieldNames()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        ArrangeBothChanged("Prod");
        _sync.State.GetOrAddCollection(Environments).ExcludedKeys.Add("Linux");
        _environments.Local["Linux"] = "l";

        // Act
        bool result = await Run("Show details", "Cancel");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        int index = lines.IndexOf("Push:");
        Assert.True(index >= 0);
        Assert.Equal([
            "Push:", "  Environments/Dev: add", "Conflict:",
            "  Environments/Prod: changed here and on the server (Value)", "Skipped:",
            "  Environments/Linux: excluded on this computer"
        ], lines.Skip(index).Take(6));
    }

    [Fact]
    public async Task RunBody_WhenDryRunIsChosen_ShowsWhatApplySendsAndSendsNothing()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Local["Prod"] = "p";
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);

        // Act
        bool result = await Run("Dry run", "Cancel");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        int index = lines.IndexOf(
            "Dry run: Apply would send 2 records to SupportToolsServer, nothing is sent now");
        Assert.True(index >= 0);
        Assert.Equal([
            "  1. add Environments/Dev", "  2. add Environments/Prod", "Apply would change 1 records on this computer",
            "  add Environments/Test"
        ], lines.Skip(index + 1).Take(4));
        Assert.Empty(_sync.Calls);
        Assert.Equal(["Test"], _environments.Server.Keys);
        _sync.VerifySaved(Times.Never());
        Assert.Equal([EnvironmentsRequest], _server.Requests.Select(x => x.Split('?')[0]));
    }

    [Fact]
    public async Task RunBody_WhenRecordIsExcluded_SavesItsKeyAndPlansAgainWithoutIt()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Local["Prod"] = "p";

        // Act
        bool result = await Run("Exclude record from sync", "Environments/Dev", "Apply");

        // Assert
        Assert.True(result);
        Assert.Contains("Dev", _sync.State.Collections[Environments].ExcludedKeys);
        Assert.Equal(["Upsert Environments/Prod/0"], _sync.Calls);
        _sync.ParametersManager.Verify(x => x.Save(_sync.Parameters,
            "Environments/Dev is excluded from the registry sync on this computer", null,
            It.IsAny<CancellationToken>()), Times.Once);
        _sync.VerifySaved(Times.Exactly(2));
        Assert.Contains("Environments                   0     1         0       0        1", ConsoleLines());
    }

    [Fact]
    public async Task RunBody_WhenTheOnlyChangeIsExcluded_EndsWithoutAskingAgain()
    {
        // Arrange
        _environments.Local["Dev"] = "d";

        // Act
        bool result = await Run("Exclude record from sync", "Environments/Dev");

        // Assert
        Assert.True(result);
        Assert.Equal(2, _shownMenus.Count);
        Assert.Empty(_sync.Calls);
        Assert.Contains("The local registry and SupportToolsServer are in sync", ConsoleText(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenExclusionIsCanceled_ExcludesAndSavesNothing()
    {
        // Arrange
        _environments.Local["Dev"] = "d";

        // Act
        bool result = await Run("Exclude record from sync", "Back", "Cancel");

        // Assert
        Assert.True(result);
        Assert.False(_sync.State.Collections.ContainsKey(Environments));
        _sync.VerifySaved(Times.Never());
    }

    //seed: hundreds of requests stop at the first rejected record, which is named
    [Fact]
    public async Task RunBody_WhenAServerOperationFails_StopsTheOtherServerOperationsAndNamesTheRecord()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.ServerErrors["Dev"] = Error.Problem("ValueTooLong", "Name Is Longer Than 50 Characters");
        _environments.Local["Prod"] = "p";
        FakeRegistrySyncAdapter servers = AddAdapter(Servers, 100);
        servers.Local["Merinson"] = "m";

        // Act
        bool result = await Run("Apply");

        // Assert
        Assert.False(result);
        Assert.Equal(["Upsert Environments/Dev/0"], _sync.Calls);
        string console = ConsoleText();
        Assert.Contains("1/3 add Environments/Dev: Failed - Name Is Longer Than 50 Characters", console,
            StringComparison.Ordinal);
        Assert.Contains("Sync result: failed 1, not executed 2", console, StringComparison.Ordinal);
        Assert.Contains("[ERROR]   failed Environments/Dev: Name Is Longer Than 50 Characters", ConsoleLines());
        Assert.Contains("[ERROR] The sync stopped after the failure of Environments/Dev: fix the record or exclude " +
                        "it from the sync, then sync again", ConsoleLines());
    }

    [Fact]
    public async Task RunBody_WhenServerBecomesUnreachable_ReportsItInsteadOfTheFailedRecord()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.ServerErrors["Dev"] = ApiClientErrors.ApiRequestFailed("http://sts/api: connection refused");
        _environments.Local["Prod"] = "p";

        // Act
        bool result = await Run("Apply");

        // Assert
        Assert.False(result);
        string console = ConsoleText();
        Assert.Contains("SupportToolsServer became unreachable, the remaining server operations were not executed: " +
                        "Api request failed: http://sts/api: connection refused", console, StringComparison.Ordinal);
        Assert.DoesNotContain("The sync stopped after the failure", console, StringComparison.Ordinal);
    }

    //another computer changed the record while the user was choosing: 409 ConcurrencyConflict
    [Fact]
    public async Task RunBody_WhenSentRecordChangedOnServerMeanwhile_ReportsTheConflict()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "mine";
        _environments.Local["Test"] = "t";
        _beforeAnswer = answer =>
        {
            if (answer == "Apply")
            {
                _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
            }
        };

        // Act
        bool result = await Run("Apply");

        // Assert
        Assert.False(result);
        string console = ConsoleText();
        Assert.Contains("1/2 update Environments/Dev: conflict - Record Dev was changed by someone else", console,
            StringComparison.Ordinal);
        Assert.Contains("Sync result: done 1, conflict 1", ConsoleLines());
        Assert.Contains("[ERROR]   conflict Environments/Dev: Record Dev was changed by someone else", ConsoleLines());
        Assert.DoesNotContain("  done Environments/Test", console, StringComparison.Ordinal);
        Assert.DoesNotContain("The sync stopped", console, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenParametersAreNotSaved_ReportsIt()
    {
        // Arrange
        _sync.ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);

        // Act
        bool result = await Run("Apply");

        // Assert
        Assert.False(result);
        Assert.Contains("The parameters were not saved", ConsoleText(), StringComparison.Ordinal);
    }

    //neither 401 nor a transport error: only the error itself, without a hint
    [Fact]
    public async Task RunBody_WhenServerAnswersWithAnotherError_ShowsTheErrorWithoutAHint()
    {
        // Arrange
        _server.Fail(EnvironmentsRequest, HttpStatusCode.InternalServerError, string.Empty);

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        string console = ConsoleText();
        Assert.Contains($"address {FakeSupportToolsServer.Address}): Api Returned an Error: 500 Internal Server Error",
            console, StringComparison.Ordinal);
        Assert.DoesNotContain("check the ApiKey", console, StringComparison.Ordinal);
        Assert.DoesNotContain("The server does not answer", console, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenTheStateOfEqualRecordsIsNotSaved_ReportsIt()
    {
        // Arrange
        _sync.ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _environments.Local["Dev"] = "d";
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 7);

        // Act
        bool result = await Run();

        // Assert
        Assert.False(result);
        Assert.Contains("The registry sync state was not saved", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenNoActionIsChosen_CancelsWithoutChanges()
    {
        // Arrange
        _environments.Local["Dev"] = "d";

        // Act
        bool result = await Run(NoAnswer);

        // Assert
        Assert.True(result);
        Assert.Empty(_sync.Calls);
        _sync.VerifySaved(Times.Never());
        Assert.Contains("Canceled, nothing was synced", ConsoleText(), StringComparison.Ordinal);
    }

    //a side that is gone: its resolution deletes the record on the other side, and its fields show as (none)
    [Fact]
    public async Task RunBody_WhenRecordWasDeletedOnServer_OffersToDeleteItHereAndShowsTheMissingSide()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "mine";
        _environments.Server.Remove("Dev");

        // Act
        bool result = await Run("Resolve conflicts", "Show differences", "Skip", "Cancel");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        Assert.Contains("Conflict 1/1: Environments/Dev - deleted on the server, changed here", lines);
        Assert.Equal([
            "Local (send this computer's version to the server)", "Server (delete it on this computer)",
            "Skip (keep the conflict for a later sync)", "Show differences (field by field, secrets are hidden)"
        ], MenuLines(_shownMenus[1]));
        int index = lines.IndexOf("Differences of Environments/Dev (secrets are hidden):");
        Assert.Equal([
            "  Name", "    local:  Dev", "    server: (none)", "  Value", "    local:  mine", "    server: (none)"
        ], lines.Skip(index + 1).Take(6));
    }

    [Fact]
    public async Task RunBody_WhenRecordWasDeletedHere_OffersToDeleteItOnTheServer()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local.Remove("Dev");
        _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);

        // Act
        bool result = await Run("Resolve conflicts", "Show differences", "Local", "Apply");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        Assert.Contains("Conflict 1/1: Environments/Dev - deleted here, changed on the server", lines);
        Assert.Equal(["Local (delete it on the server)", "Server (take the server's version to this computer)"],
            MenuLines(_shownMenus[1]).Take(2));
        int index = lines.IndexOf("Differences of Environments/Dev (secrets are hidden):");
        Assert.Equal([
            "  Name", "    local:  (none)", "    server: Dev", "  Value", "    local:  (none)", "    server: theirs"
        ], lines.Skip(index + 1).Take(6));
        Assert.Equal(["Delete Environments/Dev/4"], _sync.Calls);
        Assert.Contains("1/1 delete Environments/Dev: done", lines);
    }

    [Fact]
    public async Task RunBody_WhenRecordsAreUpdatedAndDeleted_DetailsNameTheChangesAndTheirFields()
    {
        // Arrange
        ArrangeUpdatesAndDeletes();

        // Act
        bool result = await Run("Show details", "Cancel");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        int index = lines.IndexOf("Pull:");
        Assert.Equal([
            "Pull:", "  Environments/PullGone: delete", "  Environments/PullUpd: update (Value)", "Push:",
            "  Environments/Gone: delete", "  Environments/Upd: update (Value)"
        ], lines.Skip(index).Take(6));
    }

    [Fact]
    public async Task RunBody_WhenDryRunMeetsUpdatesAndDeletes_ListsThemInExecutionOrder()
    {
        // Arrange
        ArrangeUpdatesAndDeletes();

        // Act
        bool result = await Run("Dry run", "Cancel");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        int index = lines.IndexOf(
            "Dry run: Apply would send 2 records to SupportToolsServer, nothing is sent now");
        Assert.Equal([
            "  1. update Environments/Upd", "  2. delete Environments/Gone",
            "Apply would change 2 records on this computer", "  update Environments/PullUpd",
            "  delete Environments/PullGone"
        ], lines.Skip(index + 1).Take(5));
        Assert.Empty(_sync.Calls);
    }

    [Fact]
    public async Task RunBody_WhenApplyMeetsUpdatesAndDeletes_ShowsTheProgressOfEveryServerOperation()
    {
        // Arrange
        ArrangeUpdatesAndDeletes();

        // Act
        bool result = await Run("Apply");

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        Assert.Contains("1/2 update Environments/Upd: done", lines);
        Assert.Contains("2/2 delete Environments/Gone: done", lines);
        Assert.Contains("Sync result: done 4", lines);
    }

    [Fact]
    public async Task RunBody_WhenExclusionIsNotSaved_StopsWithoutAskingAgain()
    {
        // Arrange
        _sync.ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _environments.Local["Dev"] = "d";
        _environments.Local["Prod"] = "p";

        // Act
        bool result = await Run("Exclude record from sync", "Environments/Dev");

        // Assert
        Assert.False(result);
        Assert.Equal(2, _shownMenus.Count);
        Assert.Empty(_sync.Calls);
    }

    //the server stops answering after the record is excluded: the new plan cannot be built
    [Fact]
    public async Task RunBody_WhenPlanCannotBeBuiltAgainAfterAnExclusion_ReportsTheError()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Local["Prod"] = "p";
        _beforeAnswer = answer =>
        {
            if (answer == "Environments/Dev")
            {
                _environments.ServerRecordsError = ApiClientErrors.ApiRequestFailed("http://sts/api: timeout");
            }
        };

        // Act
        bool result = await Run("Exclude record from sync", "Environments/Dev");

        // Assert
        Assert.False(result);
        Assert.Contains("The registry sync plan was not created: Api request failed: http://sts/api: timeout",
            ConsoleText(), StringComparison.Ordinal);
        Assert.Equal(2, _shownMenus.Count);
    }

    [Fact]
    public async Task RunBody_WhenTheAnswerIsPastTheLastAction_CancelsWithoutChanges()
    {
        // Arrange
        _environments.Local["Dev"] = "d";

        // Act
        bool result = await Run(OutOfRangeAnswer);

        // Assert
        Assert.True(result);
        Assert.Empty(_sync.Calls);
        Assert.Contains("Canceled, nothing was synced", ConsoleText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunBody_WhenThereAreNoConflicts_OffersNoConflictChoices()
    {
        // Arrange
        _environments.Local["Dev"] = "d";

        // Act
        bool result = await Run("Cancel");

        // Assert
        Assert.True(result);
        Assert.Equal([
            "Apply (send 1, change here 0)", "Pull only (send 0, change here 0)", "Push only (send 1, change here 0)",
            "Show details (the records to sync)", "Dry run (what Apply sends, nothing is sent)",
            "Exclude record from sync (on this computer)", "Cancel (nothing changes)"
        ], MenuLines(Assert.Single(_shownMenus)));
    }

    //Skip forgets an earlier choice: the conflict stays for a later sync
    [Fact]
    public async Task RunBody_WhenAResolvedConflictIsSkippedLater_LeavesItUnresolved()
    {
        // Arrange
        ArrangeBothChanged("Dev");

        // Act
        bool result = await Run("Resolve conflicts", "Local", "Resolve conflicts", "Skip", "Apply");

        // Assert
        Assert.True(result);
        Assert.Empty(_sync.Calls);
        Assert.Contains("Resolve conflicts (1 conflicts, 0 resolved)", MenuLines(_shownMenus[^1]));
    }

    [Fact]
    public async Task RunBody_WhenSeveralFieldsDiffer_DetailsNameAllOfThem()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local.Remove("Dev");
        _environments.Local["dev"] = "mine";
        _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);

        // Act
        bool result = await Run("Show details", "Cancel");

        // Assert
        Assert.True(result);
        Assert.Contains("  Environments/dev: changed here and on the server (Name, Value)", ConsoleLines());
    }

    //the records to sync with their action; no answer goes back like Back
    [Fact]
    public async Task RunBody_WhenExclusionIsChosen_ListsTheRecordsToSyncWithTheirActions()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        ArrangeBothChanged("Prod");

        // Act
        bool result = await Run("Exclude record from sync", NoAnswer, "Cancel");

        // Assert
        Assert.True(result);
        Assert.Equal([
            "Environments/Dev (Push: add)",
            "Environments/Prod (Conflict: changed here and on the server (Value))",
            "Back (exclude nothing)"
        ], MenuLines(_shownMenus[1]));
        Assert.Equal(["action", "record to exclude", "action"], _questions);
        Assert.Empty(_sync.State.Collections[Environments].ExcludedKeys);
        _sync.VerifySaved(Times.Never());
    }

    //the public constructor reads the choice from the console: the test host has none, so the prompt is all it shows
    [Fact]
    public async Task RunBody_WhenCreatedForTheConsole_AsksForTheActionOnTheConsole()
    {
        // Arrange
        _sync.Parameters.Environments["Production"] = "Live";
        var sut = new SyncRegistryCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _sync.ParametersManager.Object);

        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => CliMenuTestAccess.InvokeRunBody(sut));

        // Assert
        Assert.Contains("Environments                   0     1         0       0        0", ConsoleLines());
        Assert.Contains("Select action", ConsoleText(), StringComparison.Ordinal);
    }

    //the warnings of the adapters and the paths without a mapping come before the summary; they do not stop the sync
    [Fact]
    public async Task SessionRun_WhenAdaptersHaveWarningsAndPathIssues_ShowsThemBeforeTheSummary()
    {
        // Arrange
        var warnings = new RegistrySyncWarnings();
        warnings.Add("Projects", "AppFake", "GitProjectNames AppFront is not on the server");
        warnings.Add("GitIgnorePatterns", null, "FolderForGitignoreFiles is not set, the templates are not synced");
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        pathMapper.ToCanonical("/home/u/Other/appsettings.json");
        pathMapper.ToLocal(@"X:\Unmapped\appsettings.json");
        _environments.Local["Dev"] = "d";
        RegistrySyncSession sut = CreateSession(warnings, pathMapper);
        _answers.Enqueue("Cancel");

        // Act
        bool result = await sut.Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal([
            "[warning] Projects/AppFake: GitProjectNames AppFront is not on the server",
            "[warning] GitIgnorePatterns: FolderForGitignoreFiles is not set, the templates are not synced",
            "[warning] The path /home/u/Other/appsettings.json has no canonical form: add a path mapping in " +
            "Support Tools Parameters Editor",
            @"[warning] The server path X:\Unmapped\appsettings.json has no form on this computer: add a path " +
            "mapping in Support Tools Parameters Editor",
            "Registry sync plan:"
        ], ConsoleLines().Take(5));
    }

    //a new plan after an exclusion shows only the warnings it adds; the earlier ones are not repeated
    [Fact]
    public async Task SessionRun_WhenThePlanIsBuiltAgain_ShowsEveryWarningOnce()
    {
        // Arrange
        var warnings = new RegistrySyncWarnings();
        warnings.Add("GitIgnorePatterns", null, "FolderForGitignoreFiles is not set, the templates are not synced");
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        pathMapper.ToCanonical("/home/u/Other/appsettings.json");
        _environments.Local["Dev"] = "d";
        _environments.Local["Prod"] = "p";
        RegistrySyncSession sut = CreateSession(warnings, pathMapper);
        foreach (string answer in new[] { "Exclude record from sync", "Environments/Dev", "Cancel" })
        {
            _answers.Enqueue(answer);
        }

        // Act
        bool result = await sut.Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        Assert.Equal(2, lines.Count(x => x == "Registry sync plan:"));
        Assert.Single(lines, x => x.StartsWith("[warning] GitIgnorePatterns:", StringComparison.Ordinal));
        Assert.Single(lines,
            x => x.StartsWith("[warning] The path /home/u/Other/appsettings.json", StringComparison.Ordinal));
    }

    //a warning that comes up after the plan (for example a pulled path without a local form) shows after the sync
    [Fact]
    public async Task SessionRun_WhenAWarningComesUpAfterThePlan_ShowsItAfterTheSync()
    {
        // Arrange
        var warnings = new RegistrySyncWarnings();
        PathMapper pathMapper = MapperTestHelpers.LinuxPathMapper();
        _environments.Server["Test"] = new FakeContract("Test", "t", 1);
        _beforeAnswer = answer =>
        {
            if (answer == "Apply")
            {
                pathMapper.ToLocal(@"X:\Unmapped\appsettings.json");
            }
        };
        RegistrySyncSession sut = CreateSession(warnings, pathMapper);
        _answers.Enqueue("Apply");

        // Act
        bool result = await sut.Run(CancellationToken.None);

        // Assert
        Assert.True(result);
        List<string> lines = ConsoleLines();
        int warningIndex = lines.FindIndex(x =>
            x.StartsWith(@"[warning] The server path X:\Unmapped\appsettings.json", StringComparison.Ordinal));
        Assert.True(warningIndex > lines.IndexOf("Registry sync plan:"));
        Assert.True(warningIndex < lines.IndexOf("Sync result: done 1"));
    }

    private RegistrySyncSession CreateSession(RegistrySyncWarnings warnings, PathMapper pathMapper)
    {
        return new RegistrySyncSession(_sync.CreateEngine([.. _adapters]), warnings, pathMapper,
            _sync.ParametersManager.Object, InputIdFromMenuList, new Mock<ILogger>().Object);
    }

    private FakeRegistrySyncAdapter AddAdapter(string collectionName, int order)
    {
        FakeRegistrySyncAdapter adapter = _sync.CreateAdapter(collectionName, order);
        _adapters.Add(adapter);
        return adapter;
    }

    //after a sync: Upd changed here, Gone deleted here, PullUpd changed on the server, PullGone deleted on the server
    private void ArrangeUpdatesAndDeletes()
    {
        _sync.AddSyncedRecord(_environments, "Upd", "old", 3);
        _environments.Local["Upd"] = "new";
        _sync.AddSyncedRecord(_environments, "Gone", "g", 2);
        _environments.Local.Remove("Gone");
        _sync.AddSyncedRecord(_environments, "PullUpd", "old", 3);
        _environments.Server["PullUpd"] = new FakeContract("PullUpd", "new", 4);
        _sync.AddSyncedRecord(_environments, "PullGone", "p", 2);
        _environments.Server.Remove("PullGone");
    }

    //the record was synced with version 3 and value "old"; then both sides changed it
    private void ArrangeBothChanged(string key)
    {
        _sync.AddSyncedRecord(_environments, key, "old", 3);
        _environments.Local[key] = "mine";
        _environments.Server[key] = new FakeContract(key, "theirs", 4);
    }

    private async Task<bool> Run(params string[] answers)
    {
        foreach (string answer in answers)
        {
            _answers.Enqueue(answer);
        }

        var sut = new SyncRegistryCliMenuCommand(new Mock<ILogger>().Object, _httpClientFactory.Object,
            _sync.ParametersManager.Object, InputIdFromMenuList, (_, _, _, _) => _adapters);
        bool result = await CliMenuTestAccess.InvokeRunBody(sut);
        Assert.Empty(_answers);
        return result;
    }

    private int InputIdFromMenuList(string fieldName, CliMenuSet menuSet)
    {
        _shownMenus.Add(menuSet);
        _questions.Add(fieldName);
        Assert.True(_answers.Count > 0, $"Unexpected question: {fieldName}");
        string answer = _answers.Dequeue();
        _beforeAnswer?.Invoke(answer);
        if (answer == NoAnswer)
        {
            return -1;
        }

        if (answer == OutOfRangeAnswer)
        {
            return CliMenuTestAccess.GetMenuItems(menuSet).Count;
        }

        int id = CliMenuTestAccess.GetMenuItems(menuSet).FindIndex(x => x.MenuItemName == answer);
        Assert.True(id >= 0, $"{answer} is not offered for {fieldName}");
        return id;
    }

    //the menu items as the menu shows them: "Name (status)"
    private static List<string> MenuLines(CliMenuSet menuSet)
    {
        List<CliMenuItem> items = CliMenuTestAccess.GetMenuItems(menuSet);
        items.ForEach(x => x.CliMenuCommand.CountStatus());
        return [.. items.Select(x => $"{x.MenuItemName} ({x.CliMenuCommand.StatusString})")];
    }

    private string ConsoleText()
    {
        return _consoleOutput.ToString();
    }

    private List<string> ConsoleLines()
    {
        return [.. ConsoleText().Split(Environment.NewLine)];
    }
}
