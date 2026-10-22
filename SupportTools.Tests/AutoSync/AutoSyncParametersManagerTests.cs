using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using ParametersManagement.LibParameters;
using SupportTools.AutoSync;
using SupportTools.Tests.Registry.Sync;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.AutoSync;

//the IParametersManager decorator of auto-sync (D2): the local save comes first and decides the result, then the
//non-conflicting pushes run. The inner manager is a mock whose Save succeeds; the server is FakeRegistrySyncAdapter
[Collection(ConsoleCaptureCollection.Name)]
public sealed class AutoSyncParametersManagerTests : IDisposable
{
    private const string SyncRegistryAdvice = "run \"Sync Registry With SupportToolsServer...\"";

    private readonly AutoSyncTestContext _context = new();
    private readonly FakeRegistrySyncAdapter _environments;
    private readonly RegistrySyncTestContext _sync;

    public AutoSyncParametersManagerTests()
    {
        _sync = _context.Sync;
        _environments = _context.EnvironmentsAdapter;
        _sync.State.ServerAddress = AutoSyncTestContext.ServerAddress;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Save_WhenLocalSaveSucceeds_PushesTheLocalChange()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "new";
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Equal("Upsert Environments/Dev/3", Assert.Single(_sync.Calls));
        Assert.Equal("new", _environments.Server["Dev"].Value);
        Assert.Equal(4, _sync.State.Collections["Environments"].Records["Dev"].Version);
        Assert.Equal("Registry auto-sync: sent to SupportToolsServer: Environments/Dev",
            Assert.Single(_context.ConsoleLines()));
        //the local save with its message, then the state of the push without one
        _sync.ParametersManager.Verify(x => x.Save(_sync.Parameters, "Saved", null, It.IsAny<CancellationToken>()),
            Times.Once);
        _sync.ParametersManager.Verify(x => x.Save(_sync.Parameters, string.Empty, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Save_WhenLocalSaveFails_ReturnsFalseWithoutCallingTheServer()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _sync.ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.False(result);
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Empty(_sync.Calls);
    }

    //a server error never spoils the local save: Save answers true, the warning names the reason, and the session
    //goes on offline
    [Fact]
    public async Task Save_WhenServerIsUnreachable_ReturnsTrueWarnsAndStopsTryingForTheSession()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.ServerRecordsError = ApiClientErrors.ApiRequestFailed("http://sts/api/v1/environments: refused");
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool first = await sut.Save(_sync.Parameters, "Saved");
        _environments.Local["Prod"] = "p";
        bool second = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(first);
        Assert.True(second);
        Assert.Equal(1, _environments.ServerReadCount);
        Assert.StartsWith("[warning] Registry auto-sync: SupportToolsServer is not reachable (",
            Assert.Single(_context.ConsoleLines()), StringComparison.Ordinal);
        _sync.VerifySaved(Times.Exactly(2));
    }

    //both sides changed: nothing is sent, the local change and its old state stay, so the next sync shows the conflict
    [Fact]
    public async Task Save_WhenRecordChangedOnServerToo_KeepsTheLocalChangeWaiting()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "mine";
        _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Empty(_sync.Calls);
        Assert.Equal("mine", _environments.Local["Dev"]);
        Assert.Equal("theirs", _environments.Server["Dev"].Value);
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "old"),
            _sync.State.Collections["Environments"].Records["Dev"].Hash);
        Assert.Equal(
            "[warning] Registry auto-sync: the local changes of Environments/Dev (changed here and on the server) " +
            $"wait: {SyncRegistryAdvice}", Assert.Single(_context.ConsoleLines()));
    }

    //the plan saw no conflict, but another computer changed the record before the upsert (409)
    [Fact]
    public async Task Save_WhenServerAnswersConflictToThePush_KeepsTheLocalChangeWaiting()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "mine";
        _environments.ServerErrors["Dev"] = FakeRegistrySyncAdapter.ConcurrencyConflict("Dev");
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Equal("Upsert Environments/Dev/3", Assert.Single(_sync.Calls));
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "old"),
            _sync.State.Collections["Environments"].Records["Dev"].Hash);
        Assert.Equal(
            "[warning] Registry auto-sync: the local changes of Environments/Dev (changed on the server meanwhile) " +
            $"wait: {SyncRegistryAdvice}", Assert.Single(_context.ConsoleLines()));
    }

    [Fact]
    public async Task Save_WhenServerRejectsTheRecord_ReturnsTrueAndNamesTheError()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.ServerErrors["Dev"] = Error.Conflict("RecordIsInUse", "Dev is used");
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Equal("[warning] Registry auto-sync: Environments/Dev was not sent: Dev is used",
            Assert.Single(_context.ConsoleLines()));
    }

    //an exception of the push never reaches the caller of Save
    [Fact]
    public async Task Save_WhenThePushThrows_ReturnsTrueAndWarns()
    {
        // Arrange
        var adapter = new Mock<IRegistrySyncAdapter>();
        adapter.SetupGet(x => x.CollectionName).Returns("Broken");
        adapter.Setup(x => x.GetLocalRecords()).Throws(new InvalidOperationException("broken adapter"));
        _context.Adapters.Add(adapter.Object);
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Equal("[warning] Registry auto-sync: the changes were not sent to SupportToolsServer: broken adapter",
            Assert.Single(_context.ConsoleLines()));
    }

    [Fact]
    public async Task Save_WhenAutoSyncIsOff_SavesWithoutCallingTheServer()
    {
        // Arrange
        _sync.Parameters.AutoSyncWithSupportToolsServer = false;
        _environments.Local["Dev"] = "d";
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Equal(0, _environments.ServerReadCount);
        Assert.Empty(_sync.Calls);
        Assert.Empty(_context.ConsoleOutput);
        _sync.VerifySaved(Times.Once());
    }

    //the engine saves the sync state through the inner manager, not through the decorator. A save through the decorator
    //would push again: Dev is then in sync, but the waiting conflict Both is a new set of local changes, so a second
    //plan would read the server and show the conflict twice
    [Fact]
    public async Task Save_WhenThePushSavesTheSyncState_DoesNotPushAgain()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _sync.AddSyncedRecord(_environments, "Both", "old", 3);
        _environments.Local["Both"] = "mine";
        _environments.Server["Both"] = new FakeContract("Both", "theirs", 4);
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.Equal(1, _environments.ServerReadCount);
        Assert.Equal("Upsert Environments/Dev/0", Assert.Single(_sync.Calls));
        Assert.Equal(2, _context.ConsoleLines().Count);
        _sync.VerifySaved(Times.Exactly(2));
    }

    //one operation saves the file 2–3 times (ParCruder + Cruder): only the first save reaches the server
    [Fact]
    public async Task Save_WhenTheSameDataIsSavedAgain_DoesNotCallTheServer()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "new";
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        await sut.Save(_sync.Parameters, "Saved");
        await sut.Save(_sync.Parameters, "Saved");
        await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.Equal(1, _environments.ServerReadCount);
        Assert.Single(_sync.Calls);
    }

    //a waiting conflict is not tried and shown again at every save, only when the local changes change
    [Fact]
    public async Task Save_WhenTheSameChangesAlreadyWait_DoesNotCallTheServerAgain()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "mine";
        _environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        await sut.Save(_sync.Parameters, "Saved");
        await sut.Save(_sync.Parameters, "Saved");
        _environments.Local["Prod"] = "p";
        await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.Equal(2, _environments.ServerReadCount);
        Assert.Equal("Upsert Environments/Prod/0", Assert.Single(_sync.Calls));
        List<string> lines = _context.ConsoleLines();
        Assert.Equal(3, lines.Count);
        Assert.Equal("Registry auto-sync: sent to SupportToolsServer: Environments/Prod", lines[1]);
        Assert.Equal(lines[0], lines[2]);
    }

    //the local deletion is pushed as well
    [Fact]
    public async Task Save_WhenRecordWasDeletedLocally_DeletesItOnTheServer()
    {
        // Arrange
        _sync.AddSyncedRecord(_environments, "Dev", "d", 3);
        _environments.Local.Remove("Dev");
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.Equal("Delete Environments/Dev/3", Assert.Single(_sync.Calls));
        Assert.False(_environments.Server.ContainsKey("Dev"));
        Assert.False(_sync.State.Collections["Environments"].Records.ContainsKey("Dev"));
    }

    //a save of a sub-object instead of the root (a bug that CLAUDE.md warns about) has nothing to sync
    [Fact]
    public async Task Save_WhenParametersAreNotTheRoot_DoesNotCallTheServer()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _sync.ParametersManager.SetupGet(x => x.Parameters).Returns(new Mock<IParameters>().Object);
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        bool result = await sut.Save(_sync.Parameters, "Saved");

        // Assert
        Assert.True(result);
        Assert.Equal(0, _environments.ServerReadCount);
    }

    [Fact]
    public void Parameters_WhenRead_IsTheObjectOfTheInnerManager()
    {
        // Arrange
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        IParameters result = sut.Parameters;

        // Assert
        Assert.Same(_sync.Parameters, result);
    }

    [Fact]
    public void Parameters_WhenSet_SetsThemOnTheInnerManager()
    {
        // Arrange
        AutoSyncParametersManager sut = _context.CreateParametersManager();
        IParameters other = new Mock<IParameters>().Object;

        // Act
        sut.Parameters = other;

        // Assert
        _sync.ParametersManager.VerifySet(x => x.Parameters = other, Times.Once);
    }

    [Fact]
    public void ParametersFileName_WhenRead_IsTheFileOfTheInnerManager()
    {
        // Arrange
        _sync.ParametersManager.SetupGet(x => x.ParametersFileName).Returns(@"D:\Fake\SupportTools.json");
        AutoSyncParametersManager sut = _context.CreateParametersManager();

        // Act
        string? result = sut.ParametersFileName;

        // Assert
        Assert.Equal(@"D:\Fake\SupportTools.json", result);
    }

    [Fact]
    public void WithoutAutoSync_WhenGivenTheDecorator_ReturnsTheInnerManager()
    {
        // Arrange
        AutoSyncParametersManager decorator = _context.CreateParametersManager();

        // Act
        IParametersManager result = AutoSyncParametersManager.WithoutAutoSync(decorator);

        // Assert
        Assert.Same(_sync.ParametersManager.Object, result);
    }

    [Fact]
    public void WithoutAutoSync_WhenGivenAnotherManager_ReturnsIt()
    {
        // Act
        IParametersManager result = AutoSyncParametersManager.WithoutAutoSync(_sync.ParametersManager.Object);

        // Assert
        Assert.Same(_sync.ParametersManager.Object, result);
    }
}
