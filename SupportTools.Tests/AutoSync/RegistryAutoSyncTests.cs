using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.AutoSync;
using SupportTools.Tests.Registry.Sync;
using SystemTools.ApiContracts.Errors;
using Xunit;

namespace SupportTools.Tests.AutoSync;

//the start-up pull of auto-sync (D2): only the non-conflicting pulls run, deletions excluded; the rest waits for the
//manual sync, and an unreachable server only warns
[Collection(ConsoleCaptureCollection.Name)]
public sealed class RegistryAutoSyncTests : IDisposable
{
    private readonly AutoSyncTestContext _context = new();
    private readonly FakeRegistrySyncAdapter _environments;
    private readonly RegistrySyncTestContext _sync;

    public RegistryAutoSyncTests()
    {
        _sync = _context.Sync;
        _environments = _context.EnvironmentsAdapter;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task PullOnStart_WhenAutoSyncIsOff_DoesNothing()
    {
        // Arrange
        _sync.Parameters.AutoSyncWithSupportToolsServer = false;
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 1);

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Empty(_environments.Local);
        Assert.Empty(_context.ConsoleOutput);
        _sync.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task PullOnStart_WhenServerHasChanges_TakesOnlyAddsAndUpdatesAndNamesWhatWaits()
    {
        // Arrange
        _environments.Server["Added"] = new FakeContract("Added", "a", 1);
        _sync.AddSyncedRecord(_environments, "Changed", "old", 3);
        _environments.Server["Changed"] = new FakeContract("Changed", "new", 4);
        _sync.AddSyncedRecord(_environments, "Gone", "g", 2);
        _environments.Server.Remove("Gone");
        _sync.AddSyncedRecord(_environments, "Mine", "old", 1);
        _environments.Local["Mine"] = "new";
        _sync.AddSyncedRecord(_environments, "Both", "old", 3);
        _environments.Local["Both"] = "mine";
        _environments.Server["Both"] = new FakeContract("Both", "theirs", 4);

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(["ApplyLocal Environments/Added", "ApplyLocal Environments/Changed"], _sync.Calls);
        Assert.Equal("a", _environments.Local["Added"]);
        Assert.Equal("new", _environments.Local["Changed"]);
        Assert.Equal("g", _environments.Local["Gone"]);
        Assert.Equal("new", _environments.Local["Mine"]);
        Assert.Equal("mine", _environments.Local["Both"]);
        Assert.Equal(1, _environments.Server["Mine"].Version);
        Assert.Equal(
            "[warning] Registry auto-sync: 2 records taken from SupportToolsServer; run \"Sync Registry With " +
            "SupportToolsServer...\" for 1 to send, 1 deleted on the server, 1 in conflict",
            Assert.Single(_context.ConsoleLines()));
        _sync.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task PullOnStart_WhenEverythingIsInSync_WritesOneLineAndSendsNothing()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "d", 3);
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal("Registry auto-sync: nothing new on SupportToolsServer", Assert.Single(_context.ConsoleLines()));
        Assert.Empty(_sync.Calls);
        _sync.VerifySaved(Times.Never());
    }

    //the menu clears the screen when it starts, so in menu mode the summary waits for the first main menu build
    [Fact]
    public async Task PullOnStart_WhenOutputIsDeferred_WritesItOnceWhenAsked()
    {
        // Arrange
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 1);
        _sync.AddSyncedRecord(_environments, "Prod", "old", 1);
        _environments.Local["Prod"] = "new";
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PullOnStart(true);
        string beforeWrite = _context.ConsoleOutput;
        sut.WriteDeferredOutput();
        sut.WriteDeferredOutput();

        // Assert
        Assert.Empty(beforeWrite);
        Assert.Equal("d", _environments.Local["Dev"]);
        Assert.Equal(
            "[warning] Registry auto-sync: 1 record taken from SupportToolsServer; run \"Sync Registry With " +
            "SupportToolsServer...\" for 1 to send", Assert.Single(_context.ConsoleLines()));
    }

    //only the start is deferred: the output of a save is written at once
    [Fact]
    public async Task PushAfterSave_WhenStartWasDeferred_WritesAtOnce()
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        RegistryAutoSync sut = _context.CreateAutoSync();
        await sut.PullOnStart(true);
        _environments.Local["Dev"] = "d";

        // Act
        await sut.PushAfterSave();

        // Assert
        Assert.Equal("Registry auto-sync: sent to SupportToolsServer: Environments/Dev",
            Assert.Single(_context.ConsoleLines()));
    }

    //an old parameters file has no server address yet: it is accepted and recorded with the sync
    [Fact]
    public async Task PullOnStart_WhenStateHasNoServerAddress_SyncsAndRecordsTheAddress()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "d", 3);

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(AutoSyncTestContext.ServerAddress, _sync.State.ServerAddress);
        Assert.Equal(1, _environments.ServerReadCount);
        _sync.VerifySaved(Times.Once());
    }

    //the state of another server would plan the deletion of every unchanged local record: nothing is read or changed
    [Fact]
    public async Task PullOnStart_WhenStateBelongsToAnotherServer_WarnsWithoutCallingTheServer()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "d", 3);
        _environments.Server.Clear();
        _sync.State.ServerAddress = "http://old.example.test/api/v1";

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Equal("d", _environments.Local["Dev"]);
        Assert.Equal(
            "[warning] Registry auto-sync: the registry sync state belongs to SupportToolsServer " +
            "http://old.example.test/api/v1, ApiClient SupportToolsServer points to " +
            $"{AutoSyncTestContext.ServerAddress}: run \"Sync Registry With SupportToolsServer...\" to sync with " +
            "the new server", Assert.Single(_context.ConsoleLines()));
        _sync.VerifySaved(Times.Never());
    }

    [Theory]
    [InlineData("http://STS.example.test/api/v1/")]
    [InlineData(" http://sts.example.test/api/v1 ")]
    public async Task PullOnStart_WhenStoredAddressDiffersOnlyInSpelling_Syncs(string storedAddress)
    {
        // Arrange
        _sync.State.ServerAddress = storedAddress;
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 1);

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal("d", _environments.Local["Dev"]);
    }

    [Fact]
    public async Task PullOnStart_WhenApiClientIsNotSet_WarnsWithoutCallingTheServer()
    {
        // Arrange
        _sync.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Equal(
            "[warning] Registry auto-sync: SupportToolsServerWebApiClientName is not set: choose the ApiClient of " +
            "SupportToolsServer in Support Tools Parameters Editor", Assert.Single(_context.ConsoleLines()));
    }

    [Fact]
    public async Task PullOnStart_WhenApiClientDoesNotExist_WarnsWithoutCallingTheServer()
    {
        // Arrange
        _sync.Parameters.SupportToolsServerWebApiClientName = "MissingClient";

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Equal(
            "[warning] Registry auto-sync: ApiClient MissingClient (SupportToolsServerWebApiClientName) does not " +
            "exist", Assert.Single(_context.ConsoleLines()));
    }

    [Fact]
    public async Task PullOnStart_WhenServerAddressIsNotSet_WarnsWithoutCallingTheServer()
    {
        // Arrange
        _sync.Parameters.ApiClients[AutoSyncTestContext.ApiClientName].Server = " ";

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Equal("[warning] Registry auto-sync: the server address of ApiClient SupportToolsServer is not set",
            Assert.Single(_context.ConsoleLines()));
    }

    //offline: the program goes on with the local copy, and the saves of this session no longer wait for the server
    [Fact]
    public async Task PullOnStart_WhenServerIsUnreachable_WarnsKeepsTheLocalCopyAndSuspendsTheSession()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "d", 3);
        _environments.ServerRecordsError = ApiClientErrors.ApiRequestFailed("http://sts/api/v1/environments: refused");
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PullOnStart(false);
        _environments.Local["Dev"] = "changed";
        await sut.PushAfterSave();

        // Assert
        Assert.Equal(1, _environments.ServerReadCount);
        Assert.Equal("changed", _environments.Local["Dev"]);
        string warning = Assert.Single(_context.ConsoleLines());
        Assert.StartsWith("[warning] Registry auto-sync: SupportToolsServer is not reachable (", warning,
            StringComparison.Ordinal);
        Assert.EndsWith(
            "), working with the local copy. Auto-sync is off until SupportTools restarts, run " +
            "\"Sync Registry With SupportToolsServer...\" when the server is back", warning, StringComparison.Ordinal);
        _sync.VerifySaved(Times.Never());
    }

    //another error (e.g. a refused API key) is shown, but the next save tries again
    [Fact]
    public async Task PullOnStart_WhenServerRefusesTheRequest_WarnsWithoutSuspending()
    {
        // Arrange
        _environments.ServerRecordsError = ApiClientErrors.ApiReturnedAnError("401 Unauthorized");
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PullOnStart(false);
        _environments.Local["Dev"] = "d";
        await sut.PushAfterSave();

        // Assert
        Assert.Equal(2, _environments.ServerReadCount);
        List<string> lines = _context.ConsoleLines();
        Assert.Equal(2, lines.Count);
        Assert.All(lines,
            x => Assert.StartsWith("[warning] Registry auto-sync: the sync plan was not created: ", x,
                StringComparison.Ordinal));
    }

    //a pulled record that the adapter cannot apply is named in the summary
    [Fact]
    public async Task PullOnStart_WhenPulledRecordIsNotApplied_CountsItAsNotTaken()
    {
        // Arrange
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 1);
        _environments.IgnoresApplyLocal = true;

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(
            "[warning] Registry auto-sync: nothing new on SupportToolsServer; run \"Sync Registry With " +
            "SupportToolsServer...\" for 1 not taken", Assert.Single(_context.ConsoleLines()));
    }

    [Fact]
    public async Task PullOnStart_WhenAdapterThrows_WarnsAndReturns()
    {
        // Arrange
        var adapter = new Mock<IRegistrySyncAdapter>();
        adapter.SetupGet(x => x.CollectionName).Returns("Broken");
        adapter.Setup(x => x.GetServerRecords(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("broken adapter"));
        _context.Adapters.Add(adapter.Object);

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal("[warning] Registry auto-sync: the registry was not taken from SupportToolsServer: broken adapter",
            Assert.Single(_context.ConsoleLines()));
    }

    //every request of the sync waits at most SupportToolsServerSyncTimeoutSeconds; an unset or wrong value is the
    //default 10 seconds
    [Theory]
    [InlineData(3, 3)]
    [InlineData(0, 10)]
    [InlineData(-5, 10)]
    public async Task PullOnStart_WhenTimeoutIsSet_GivesItToTheRequests(int timeoutSeconds, int expectedSeconds)
    {
        // Arrange
        _sync.Parameters.SupportToolsServerSyncTimeoutSeconds = timeoutSeconds;

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), Assert.Single(_context.CreatedHttpClients).Timeout);
    }

    //a problem of the parameters is shown at the start and not again at every save while it stays the same
    [Fact]
    public async Task PushAfterSave_WhenStartShowedTheSameProblem_DoesNotRepeatIt()
    {
        // Arrange
        _sync.Parameters.SupportToolsServerWebApiClientName = "MissingClient";
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PullOnStart(false);
        await sut.PushAfterSave();
        await sut.PushAfterSave();

        // Assert
        Assert.Single(_context.ConsoleLines());
    }

    [Fact]
    public async Task PushAfterSave_WhenProblemChanges_ShowsTheNewOne()
    {
        // Arrange
        _sync.Parameters.SupportToolsServerWebApiClientName = "MissingClient";
        RegistryAutoSync sut = _context.CreateAutoSync();
        await sut.PullOnStart(false);
        _sync.Parameters.SupportToolsServerWebApiClientName = null;

        // Act
        await sut.PushAfterSave();

        // Assert
        List<string> lines = _context.ConsoleLines();
        Assert.Equal(2, lines.Count);
        Assert.Contains("SupportToolsServerWebApiClientName is not set", lines[1], StringComparison.Ordinal);
    }

    //a problem that was fixed and comes back later is shown again
    [Fact]
    public async Task PushAfterSave_WhenProblemComesBackAfterAFix_ShowsItAgain()
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        _sync.Parameters.SupportToolsServerWebApiClientName = "MissingClient";
        RegistryAutoSync sut = _context.CreateAutoSync();
        await sut.PushAfterSave();
        _sync.Parameters.SupportToolsServerWebApiClientName = AutoSyncTestContext.ApiClientName;
        _environments.Local["Dev"] = "d";
        await sut.PushAfterSave();
        _sync.Parameters.SupportToolsServerWebApiClientName = "MissingClient";

        // Act
        await sut.PushAfterSave();

        // Assert
        List<string> lines = _context.ConsoleLines();
        Assert.Equal(3, lines.Count);
        Assert.Equal(lines[0], lines[2]);
        Assert.Equal("Registry auto-sync: sent to SupportToolsServer: Environments/Dev", lines[1]);
    }

    [Theory]
    [InlineData(1, "1 warning")]
    [InlineData(2, "2 warnings")]
    public async Task PullOnStart_WhenAdaptersWarn_CountsTheWarnings(int warningsCount, string expectedText)
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        for (int i = 0; i < warningsCount; i++)
        {
            _context.AdapterWarnings.Add(("Projects", $"App{i}", "The path X:\\Fake has no canonical form"));
        }

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal(
            "[warning] Registry auto-sync: nothing new on SupportToolsServer; run \"Sync Registry With " +
            $"SupportToolsServer...\" for {expectedText}", Assert.Single(_context.ConsoleLines()));
    }

    [Fact]
    public async Task PullOnStart_WhenTheTakenRecordsCannotBeSaved_Warns()
    {
        // Arrange
        _environments.Server["Dev"] = new FakeContract("Dev", "d", 1);
        _sync.ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        await _context.CreateAutoSync().PullOnStart(false);

        // Assert
        Assert.Equal([
            "Registry auto-sync: 1 record taken from SupportToolsServer",
            "[warning] Registry auto-sync: the parameters were not saved after the sync"
        ], _context.ConsoleLines());
    }

    //the server answered the plan but not the download of a file (C6): the session goes on offline
    [Fact]
    public async Task PullOnStart_WhenServerStopsAnsweringDuringThePull_SuspendsAutoSync()
    {
        // Arrange
        FakeFileRegistrySyncAdapter storedFiles = _sync.CreateFileAdapter("StoredFiles", 20);
        storedFiles.Inner.Server[@"D:\Secrets\a.json"] = new FakeContract(@"D:\Secrets\a.json", "a", 1);
        storedFiles.PrepareErrors[@"D:\Secrets\a.json"] =
            ApiClientErrors.ApiRequestFailed("http://sts/api/v1/files/content: refused");
        _context.Adapters.Add(storedFiles);
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PullOnStart(false);
        _environments.Local["Dev"] = "d";
        await sut.PushAfterSave();

        // Assert
        List<string> lines = _context.ConsoleLines();
        Assert.Equal(2, lines.Count);
        Assert.Equal(
            "[warning] Registry auto-sync: nothing new on SupportToolsServer; run \"Sync Registry With " +
            "SupportToolsServer...\" for 1 not taken", lines[0]);
        Assert.StartsWith("[warning] Registry auto-sync: SupportToolsServer is not reachable (", lines[1],
            StringComparison.Ordinal);
        Assert.Equal(1, _environments.ServerReadCount);
    }

    [Theory]
    [InlineData(5, "Environments/R1, Environments/R2, Environments/R3, Environments/R4, Environments/R5")]
    [InlineData(6, "6 records")]
    public async Task PushAfterSave_WhenRecordsAreSent_NamesUpToFiveOfThem(int recordsCount, string expectedText)
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        for (int i = 1; i <= recordsCount; i++)
        {
            _environments.Local[$"R{i}"] = $"value {i}";
        }

        // Act
        await _context.CreateAutoSync().PushAfterSave();

        // Assert
        Assert.Equal($"Registry auto-sync: sent to SupportToolsServer: {expectedText}",
            Assert.Single(_context.ConsoleLines()));
    }

    //the plan was read, then the push itself found no server: the session goes on offline
    [Fact]
    public async Task PushAfterSave_WhenServerStopsAnsweringDuringThePush_SuspendsAutoSync()
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        _environments.Local["Dev"] = "d";
        _environments.ServerErrors["Dev"] =
            ApiClientErrors.ApiRequestFailed("http://sts/api/v1/environments/update/Dev: refused");
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PushAfterSave();
        _environments.Local["Prod"] = "p";
        await sut.PushAfterSave();

        // Assert
        Assert.Equal(1, _environments.ServerReadCount);
        List<string> lines = _context.ConsoleLines();
        Assert.Equal(2, lines.Count);
        Assert.StartsWith("[warning] Registry auto-sync: Environments/Dev was not sent: Api request failed", lines[0],
            StringComparison.Ordinal);
        Assert.StartsWith("[warning] Registry auto-sync: SupportToolsServer is not reachable (", lines[1],
            StringComparison.Ordinal);
    }

    //the plan would refuse such keys anyway: the error is shown once and the server is not asked
    [Fact]
    public async Task PushAfterSave_WhenLocalKeysDifferOnlyByCase_WarnsOnceWithoutCallingTheServer()
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        _environments.Local["Dev"] = "d";
        _environments.Local["DEV"] = "D";
        RegistryAutoSync sut = _context.CreateAutoSync();

        // Act
        await sut.PushAfterSave();
        await sut.PushAfterSave();

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Equal("[warning] Registry auto-sync: Environments: local keys differ only by case: Dev/DEV",
            Assert.Single(_context.ConsoleLines()));
    }

    //the waiting change was undone (nothing left to send) and then made again: it is tried again
    [Fact]
    public async Task PushAfterSave_WhenWaitingChangeIsUndoneAndMadeAgain_TriesItAgain()
    {
        // Arrange
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "mine";
        _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        RegistryAutoSync sut = _context.CreateAutoSync();
        await sut.PushAfterSave();
        _environments.Local["Dev"] = "old";
        await sut.PushAfterSave();
        _environments.Local["Dev"] = "mine";

        // Act
        await sut.PushAfterSave();

        // Assert
        Assert.Equal(2, _environments.ServerReadCount);
        Assert.Equal(2, _context.ConsoleLines().Count);
    }

    [Fact]
    public async Task PushAfterSave_WhenAutoSyncIsOff_DoesNothing()
    {
        // Arrange
        _sync.Parameters.AutoSyncWithSupportToolsServer = false;
        _environments.Local["Dev"] = "d";

        // Act
        await _context.CreateAutoSync().PushAfterSave();

        // Assert
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Empty(_context.ConsoleOutput);
    }
}
