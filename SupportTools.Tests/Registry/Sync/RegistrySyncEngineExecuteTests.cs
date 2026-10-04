using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

public sealed class RegistrySyncEngineExecuteTests
{
    private const string Environments = "Environments";
    private const string Servers = "Servers";
    private const string Projects = "Projects";

    private readonly RegistrySyncTestContext _context = new();

    //server operations first: upserts by ascending Order, deletes by descending Order; then the local changes:
    //ApplyLocal by ascending Order, RemoveLocal by descending Order
    [Fact]
    public async Task Execute_WhenAllKindsOfOperationsArePlanned_RunsThemInDependencyOrder()
    {
        // Arrange
        FakeRegistrySyncAdapter projects = _context.CreateAdapter(Projects, 3);
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        FakeRegistrySyncAdapter servers = _context.CreateAdapter(Servers, 2);
        List<FakeRegistrySyncAdapter> adapters = [projects, environments, servers];
        foreach (FakeRegistrySyncAdapter adapter in adapters)
        {
            //Push(Add)
            adapter.Local["PushAdd"] = "new";
            //Pull(Add)
            adapter.Server["PullAdd"] = new FakeContract("PullAdd", "new", 1);
            //Push(Delete): deleted locally, unchanged on the server
            adapter.Server["PushDelete"] = new FakeContract("PushDelete", "old", 4);
            _context.SetState(adapter.CollectionName, "PushDelete", "old", 4);
            //Pull(Delete): deleted on the server, unchanged locally
            adapter.Local["PullDelete"] = "old";
            _context.SetState(adapter.CollectionName, "PullDelete", "old", 2);
        }

        RegistrySyncEngine sut = _context.CreateEngine(projects, environments, servers);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        string[] expected =
        [
            "Upsert Environments/PushAdd/0", "Upsert Servers/PushAdd/0", "Upsert Projects/PushAdd/0",
            "Delete Projects/PushDelete/4", "Delete Servers/PushDelete/4", "Delete Environments/PushDelete/4",
            "ApplyLocal Environments/PullAdd", "ApplyLocal Servers/PullAdd", "ApplyLocal Projects/PullAdd",
            "RemoveLocal Projects/PullDelete", "RemoveLocal Servers/PullDelete", "RemoveLocal Environments/PullDelete"
        ];
        Assert.Equal(expected, _context.Calls);
        Assert.Equal(12, result.Items.Count);
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.Done, x.Outcome));
    }

    [Fact]
    public async Task Execute_AfterPush_StoresNewServerVersionAndLocalHash()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "old", 3);
        environments.Local["Dev"] = "new";
        environments.Local["Test"] = "t";
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        string[] expectedCalls = ["Upsert Environments/Dev/3", "Upsert Environments/Test/0"];
        Assert.Equal(expectedCalls, _context.Calls);
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.Done, x.Outcome));
        Dictionary<string, RegistryRecordSyncStateModel> records = _context.State.Collections[Environments].Records;
        Assert.Equal(4, records["Dev"].Version);
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "new"), records["Dev"].Hash);
        Assert.Equal(1, records["Test"].Version);
        Assert.Equal(RegistrySyncTestContext.HashOf("Test", "t"), records["Test"].Hash);
        Assert.Equal("new", environments.Server["Dev"].Value);
    }

    [Fact]
    public async Task Execute_AfterPull_StoresServerVersionAndHashOfTheLocalRecord()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "old", 3);
        environments.Server["Dev"] = new FakeContract("Dev", "new", 5);
        environments.Server["Test"] = new FakeContract("Test", "t", 2);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.Done, x.Outcome));
        Assert.Equal("new", environments.Local["Dev"]);
        Assert.Equal("t", environments.Local["Test"]);
        Dictionary<string, RegistryRecordSyncStateModel> records = _context.State.Collections[Environments].Records;
        Assert.Equal(5, records["Dev"].Version);
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "new"), records["Dev"].Hash);
        Assert.Equal(2, records["Test"].Version);
    }

    [Fact]
    public async Task Execute_AfterDeletes_RemovesTheRecordsFromState()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        _context.AddSyncedRecord(environments, "Test", "t", 2);
        environments.Local.Remove("Dev");
        environments.Server.Remove("Test");
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        string[] expectedCalls = ["Delete Environments/Dev/3", "RemoveLocal Environments/Test"];
        Assert.Equal(expectedCalls, _context.Calls);
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.Done, x.Outcome));
        Assert.Empty(_context.State.Collections[Environments].Records);
        Assert.Empty(environments.Local);
        Assert.Empty(environments.Server);
    }

    //another computer saved the record after the plan was made: the server answers 409 ConcurrencyConflict
    [Fact]
    public async Task Execute_WhenPushedRecordChangedOnServerMeanwhile_ReportsConflictAndContinues()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "old", 3);
        environments.Local["Dev"] = "mine";
        environments.Local["Test"] = "t";
        RegistrySyncEngine sut = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);

        // Act
        RegistrySyncReport result = await sut.Execute(plan, RegistrySyncSelection.AllNonConflicting);

        // Assert
        RegistrySyncReportItem dev = RegistrySyncTestContext.ReportItem(result, Environments, "Dev");
        Assert.Equal(ERegistrySyncOutcome.Conflict, dev.Outcome);
        Assert.Equal("ConcurrencyConflict", dev.Error?.Code);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Environments, "Test").Outcome);
        Assert.Equal("theirs", environments.Server["Dev"].Value);
        //the record keeps the state of the last sync, so the next plan shows the conflict
        Assert.Equal(3, _context.State.Collections[Environments].Records["Dev"].Version);
        RegistrySyncPlan nextPlan = await RegistrySyncTestContext.CreatePlan(sut);
        Assert.Equal(ERegistrySyncConflict.BothChanged,
            RegistrySyncTestContext.PlanItem(nextPlan, Environments, "Dev").Conflict);
    }

    //the record was deleted on the server after the plan was made: the update answers 404 RecordWithNameNotFound
    [Fact]
    public async Task Execute_WhenUpdatedRecordWasDeletedOnServerMeanwhile_ReportsConflict()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "old", 3);
        environments.Local["Dev"] = "mine";
        RegistrySyncEngine sut = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        environments.Server.Remove("Dev");

        // Act
        RegistrySyncReport result = await sut.Execute(plan, RegistrySyncSelection.AllNonConflicting);

        // Assert
        RegistrySyncReportItem dev = Assert.Single(result.Items);
        Assert.Equal(ERegistrySyncOutcome.Conflict, dev.Outcome);
        Assert.Equal("RecordWithNameNotFound", dev.Error?.Code);
        Assert.Equal(3, _context.State.Collections[Environments].Records["Dev"].Version);
        RegistrySyncPlan nextPlan = await RegistrySyncTestContext.CreatePlan(sut);
        Assert.Equal(ERegistrySyncConflict.DeletedOnServer, Assert.Single(nextPlan.Items).Conflict);
    }

    //a delete that finds the record already gone reached its goal
    [Fact]
    public async Task Execute_WhenDeletedRecordIsAlreadyGoneFromServer_ForgetsTheRecord()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        environments.Local.Remove("Dev");
        RegistrySyncEngine sut = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        environments.Server.Remove("Dev");

        // Act
        RegistrySyncReport result = await sut.Execute(plan, RegistrySyncSelection.AllNonConflicting);

        // Assert
        RegistrySyncReportItem dev = Assert.Single(result.Items);
        Assert.Equal(ERegistrySyncOutcome.Done, dev.Outcome);
        Assert.Null(dev.Error);
        Assert.Empty(_context.State.Collections[Environments].Records);
        _context.VerifySaved(Times.Once());
    }

    //the server stops answering in the middle: the remaining server operations do not run, the completed ones and
    //the local changes are kept and saved
    [Fact]
    public async Task Execute_WhenServerBecomesUnreachable_StopsRemainingServerOperationsAndKeepsCompletedOnes()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        FakeRegistrySyncAdapter servers = _context.CreateAdapter(Servers, 2);
        environments.Local["Dev"] = "d";
        environments.Local["Prod"] = "p";
        environments.ServerErrors["Prod"] = ApiClientErrors.ApiRequestFailed("http://sts/api: connection refused");
        environments.Server["Test"] = new FakeContract("Test", "t", 1);
        servers.Local["Merinson"] = "m";
        _context.AddSyncedRecord(servers, "Old", "o", 2);
        servers.Local.Remove("Old");
        RegistrySyncEngine sut = _context.CreateEngine(environments, servers);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        string[] expectedCalls =
            ["Upsert Environments/Dev/0", "Upsert Environments/Prod/0", "ApplyLocal Environments/Test"];
        Assert.Equal(expectedCalls, _context.Calls);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Environments, "Dev").Outcome);
        RegistrySyncReportItem prod = RegistrySyncTestContext.ReportItem(result, Environments, "Prod");
        Assert.Equal(ERegistrySyncOutcome.Failed, prod.Outcome);
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, prod.Error?.Code);
        Assert.Equal(ERegistrySyncOutcome.NotExecuted,
            RegistrySyncTestContext.ReportItem(result, Servers, "Merinson").Outcome);
        Assert.Equal(ERegistrySyncOutcome.NotExecuted,
            RegistrySyncTestContext.ReportItem(result, Servers, "Old").Outcome);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Environments, "Test").Outcome);
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.TransportError?.Code);
        Dictionary<string, RegistryRecordSyncStateModel> records = _context.State.Collections[Environments].Records;
        Assert.Equal(1, records["Dev"].Version);
        Assert.False(records.ContainsKey("Prod"));
        Assert.Equal(1, records["Test"].Version);
        Assert.True(_context.State.Collections[Servers].Records.ContainsKey("Old"));
        _context.VerifySaved(Times.Once());
    }

    //for example a server that a project still uses: the server refuses the delete with 409 RecordIsInUse
    [Fact]
    public async Task Execute_WhenServerRejectsRecord_ReportsFailedAndContinues()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        FakeRegistrySyncAdapter servers = _context.CreateAdapter(Servers, 2);
        _context.AddSyncedRecord(servers, "Merinson", "m", 2);
        servers.Local.Remove("Merinson");
        servers.ServerErrors["Merinson"] = Error.Conflict("RecordIsInUse", "Server Merinson Is Used By Projects: App");
        _context.AddSyncedRecord(environments, "Test", "t", 1);
        environments.Local.Remove("Test");
        RegistrySyncEngine sut = _context.CreateEngine(environments, servers);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        string[] expectedCalls = ["Delete Servers/Merinson/2", "Delete Environments/Test/1"];
        Assert.Equal(expectedCalls, _context.Calls);
        RegistrySyncReportItem merinson = RegistrySyncTestContext.ReportItem(result, Servers, "Merinson");
        Assert.Equal(ERegistrySyncOutcome.Failed, merinson.Outcome);
        Assert.Equal("RecordIsInUse", merinson.Error?.Code);
        Assert.Null(result.TransportError);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Environments, "Test").Outcome);
        Assert.True(_context.State.Collections[Servers].Records.ContainsKey("Merinson"));
        Assert.Empty(_context.State.Collections[Environments].Records);
    }

    [Fact]
    public async Task Execute_WhenSeveralRecordsChange_SavesTheRootParametersOnce()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "old", 3);
        environments.Local["Dev"] = "new";
        environments.Local["Prod"] = "p";
        environments.Server["Test"] = new FakeContract("Test", "t", 1);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.True(result.Changed);
        Assert.True(result.Saved);
        Assert.Equal(RegistrySyncTestContext.Now.UtcDateTime, _context.State.LastSyncUtc);
        //an empty message: the engine saves silently, the sync command shows its own report
        _context.ParametersManager.Verify(
            x => x.Save(_context.Parameters, string.Empty, null, It.IsAny<CancellationToken>()), Times.Once);
        _context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Execute_WhenEverythingIsInSync_DoesNotSave()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        _context.AddSyncedRecord(environments, "Prod", "p", 1);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.None, x.Outcome));
        Assert.False(result.Changed);
        Assert.False(result.Saved);
        Assert.Null(_context.State.LastSyncUtc);
        Assert.Empty(_context.Calls);
        _context.VerifySaved(Times.Never());
    }

    //the first sync finds equal records on both sides: only the state is written
    [Fact]
    public async Task Execute_WhenFirstSyncFindsEqualRecords_RecordsTheirStateAndSaves()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "d";
        environments.Server["Dev"] = new FakeContract("Dev", "d", 7);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result = await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.PullOnly);

        // Assert
        Assert.Equal(ERegistrySyncOutcome.None, Assert.Single(result.Items).Outcome);
        RegistryRecordSyncStateModel record = _context.State.Collections[Environments].Records["Dev"];
        Assert.Equal(7, record.Version);
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "d"), record.Hash);
        Assert.Empty(_context.Calls);
        _context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Execute_WhenRecordWasDeletedOnBothSides_RemovesItFromState()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        environments.Local.Remove("Dev");
        environments.Server.Remove("Dev");
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result = await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.PushOnly);

        // Assert
        Assert.Equal(ERegistrySyncOutcome.None, Assert.Single(result.Items).Outcome);
        Assert.Empty(_context.State.Collections[Environments].Records);
        Assert.Empty(_context.Calls);
        _context.VerifySaved(Times.Once());
    }

    //another computer pushed the same content: only the version of the state moves
    [Fact]
    public async Task Execute_WhenServerVersionMovedWithoutContentChange_UpdatesTheStateVersion()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        environments.Server["Dev"] = new FakeContract("Dev", "d", 5);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal(ERegistrySyncOutcome.None, Assert.Single(result.Items).Outcome);
        RegistryRecordSyncStateModel record = _context.State.Collections[Environments].Records["Dev"];
        Assert.Equal(5, record.Version);
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "d"), record.Hash);
        Assert.Empty(_context.Calls);
        _context.VerifySaved(Times.Once());
    }

    //the local record was changed to what the server already has: only the hash of the state moves
    [Fact]
    public async Task Execute_WhenLocalRecordChangedToTheServerContent_UpdatesTheStateHash()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "old", 3);
        environments.Local["Dev"] = "new";
        environments.Server["Dev"] = new FakeContract("Dev", "new", 3);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal(ERegistrySyncOutcome.None, Assert.Single(result.Items).Outcome);
        RegistryRecordSyncStateModel record = _context.State.Collections[Environments].Records["Dev"];
        Assert.Equal(3, record.Version);
        Assert.Equal(RegistrySyncTestContext.HashOf("Dev", "new"), record.Hash);
        Assert.Empty(_context.Calls);
        _context.VerifySaved(Times.Once());
    }

    //if the adapter still shows the removed record, its state stays: otherwise the next sync would push it back
    [Fact]
    public async Task Execute_WhenRemovedRecordStillAppearsLocally_ReportsFailedAndKeepsTheState()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        environments.Server.Remove("Dev");
        environments.IgnoresRemoveLocal = true;
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal("RemoveLocal Environments/Dev", Assert.Single(_context.Calls));
        RegistrySyncReportItem dev = Assert.Single(result.Items);
        Assert.Equal(ERegistrySyncOutcome.Failed, dev.Outcome);
        Assert.Equal(nameof(RegistrySyncErrors.LocalRecordNotRemoved), dev.Error?.Code);
        Assert.Equal(3, _context.State.Collections[Environments].Records["Dev"].Version);
        //RemoveLocal ran, so whatever it changed in the local data is saved
        _context.VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Execute_WhenOnlyPullsAreSelected_DoesNotPush()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Mine"] = "m";
        environments.Server["Theirs"] = new FakeContract("Theirs", "t", 1);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result = await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.PullOnly);

        // Assert
        Assert.Equal("ApplyLocal Environments/Theirs", Assert.Single(_context.Calls));
        Assert.Equal(ERegistrySyncOutcome.NotSelected,
            RegistrySyncTestContext.ReportItem(result, Environments, "Mine").Outcome);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Environments, "Theirs").Outcome);
        Assert.False(_context.State.Collections[Environments].Records.ContainsKey("Mine"));
    }

    [Fact]
    public async Task Execute_WhenOnlyPushesAreSelected_DoesNotPull()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Mine"] = "m";
        environments.Server["Theirs"] = new FakeContract("Theirs", "t", 1);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result = await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.PushOnly);

        // Assert
        Assert.Equal("Upsert Environments/Mine/0", Assert.Single(_context.Calls));
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Environments, "Mine").Outcome);
        Assert.Equal(ERegistrySyncOutcome.NotSelected,
            RegistrySyncTestContext.ReportItem(result, Environments, "Theirs").Outcome);
        Assert.False(environments.Local.ContainsKey("Theirs"));
    }

    [Theory]
    [InlineData(ERegistrySyncConflict.BothChanged, ERegistryConflictResolution.Local, "Upsert Environments/Dev/4")]
    [InlineData(ERegistrySyncConflict.BothChanged, ERegistryConflictResolution.Server, "ApplyLocal Environments/Dev")]
    [InlineData(ERegistrySyncConflict.DeletedOnServer, ERegistryConflictResolution.Local, "Upsert Environments/Dev/0")]
    [InlineData(ERegistrySyncConflict.DeletedOnServer, ERegistryConflictResolution.Server,
        "RemoveLocal Environments/Dev")]
    [InlineData(ERegistrySyncConflict.DeletedLocally, ERegistryConflictResolution.Local, "Delete Environments/Dev/4")]
    [InlineData(ERegistrySyncConflict.DeletedLocally, ERegistryConflictResolution.Server,
        "ApplyLocal Environments/Dev")]
    [InlineData(ERegistrySyncConflict.FirstSyncDiffers, ERegistryConflictResolution.Local, "Upsert Environments/Dev/4")]
    [InlineData(ERegistrySyncConflict.FirstSyncDiffers, ERegistryConflictResolution.Server,
        "ApplyLocal Environments/Dev")]
    public async Task Execute_WhenConflictIsResolved_MakesBothSidesEqualToTheChosenSide(ERegistrySyncConflict conflict,
        ERegistryConflictResolution resolution, string expectedCall)
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        ArrangeConflict(environments, conflict);
        RegistrySyncEngine sut = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        RegistrySyncPlanItem item = Assert.Single(plan.Items);
        Assert.Equal(conflict, item.Conflict);
        var selection = new RegistrySyncSelection
        {
            ConflictResolutions =
                new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution> { [item] = resolution }
        };

        // Act
        RegistrySyncReport result = await sut.Execute(plan, selection);

        // Assert
        Assert.Equal(expectedCall, Assert.Single(_context.Calls));
        Assert.Equal(ERegistrySyncOutcome.Done, Assert.Single(result.Items).Outcome);
        RegistrySyncPlan nextPlan = await RegistrySyncTestContext.CreatePlan(sut);
        Assert.All(nextPlan.Items, x => Assert.Equal(ERegistrySyncAction.InSync, x.Action));
    }

    [Fact]
    public async Task Execute_WhenConflictIsNotResolved_LeavesBothSidesAndStateUnchanged()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        ArrangeConflict(environments, ERegistrySyncConflict.BothChanged);
        RegistrySyncEngine sut = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        var selection = new RegistrySyncSelection
        {
            IncludePulls = true,
            IncludePushes = true,
            ConflictResolutions = new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution>
            {
                [plan.Items[0]] = ERegistryConflictResolution.Skip
            }
        };

        // Act
        RegistrySyncReport result = await sut.Execute(plan, selection);

        // Assert
        Assert.Equal(ERegistrySyncOutcome.NotSelected, Assert.Single(result.Items).Outcome);
        Assert.Empty(_context.Calls);
        Assert.Equal("mine", environments.Local["Dev"]);
        Assert.Equal(3, _context.State.Collections[Environments].Records["Dev"].Version);
        _context.VerifySaved(Times.Never());
    }

    //ExcludedKeys: for example a database connection that differs on the Linux computer
    [Fact]
    public async Task Execute_WhenKeysAreExcluded_DoesNotTouchTheRecords()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        ArrangeConflict(environments, ERegistrySyncConflict.BothChanged);
        environments.Local["LinuxDb"] = "local only";
        HashSet<string> excludedKeys = _context.State.GetOrAddCollection(Environments).ExcludedKeys;
        excludedKeys.Add("dev");
        excludedKeys.Add("LINUXDB");
        RegistrySyncEngine sut = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        var selection = new RegistrySyncSelection
        {
            IncludePulls = true,
            IncludePushes = true,
            ConflictResolutions = plan.Items.ToDictionary(x => x, _ => ERegistryConflictResolution.Local)
        };

        // Act
        RegistrySyncReport result = await sut.Execute(plan, selection);

        // Assert
        Assert.All(plan.Items, x => Assert.Equal(ERegistrySyncAction.Skipped, x.Action));
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.None, x.Outcome));
        Assert.Empty(_context.Calls);
        Assert.False(environments.Server.ContainsKey("LinuxDb"));
        Assert.Equal(3, _context.State.Collections[Environments].Records["Dev"].Version);
        _context.VerifySaved(Times.Never());
    }

    //G8: the local key, the server key and the state key differ by case, but they are one record. ApplyLocal gets the
    //local spelling, so the existing local record is updated, and the state keeps one entry
    [Fact]
    public async Task Execute_WhenKeysDifferOnlyByCase_UpdatesTheExistingLocalRecordAndState()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["dev"] = "old";
        environments.Server["DEV"] = new FakeContract("DEV", "new", 2);
        _context.State.GetOrAddCollection(Environments).Records["Dev"] = new RegistryRecordSyncStateModel
        {
            Version = 1, Hash = RegistrySyncTestContext.HashOf("dev", "old")
        };
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal("ApplyLocal Environments/dev", Assert.Single(_context.Calls));
        Assert.Equal(ERegistrySyncOutcome.Done, Assert.Single(result.Items).Outcome);
        KeyValuePair<string, string?> local = Assert.Single(environments.Local);
        Assert.Equal("dev", local.Key);
        Assert.Equal("new", local.Value);
        KeyValuePair<string, RegistryRecordSyncStateModel> state =
            Assert.Single(_context.State.Collections[Environments].Records);
        Assert.Equal("Dev", state.Key);
        Assert.Equal(2, state.Value.Version);
        //the state holds the hash of the local record, so the next sync finds nothing to do
        Assert.Equal(RegistrySyncTestContext.HashOf("dev", "new"), state.Value.Hash);
        RegistrySyncPlan nextPlan = await RegistrySyncTestContext.CreatePlan(sut);
        Assert.Equal(ERegistrySyncAction.InSync, Assert.Single(nextPlan.Items).Action);
    }

    //menu editors keep references to these objects: the sync changes them in place
    [Fact]
    public async Task Execute_WhenRecordsChange_KeepsTheLocalAndStateDictionaryInstances()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        environments.Server["Test"] = new FakeContract("Test", "t", 1);
        Dictionary<string, string?> local = environments.Local;
        Dictionary<string, RegistryCollectionSyncStateModel> collections = _context.State.Collections;
        Dictionary<string, RegistryRecordSyncStateModel> records = collections[Environments].Records;
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Same(local, environments.Local);
        Assert.Same(collections, _context.State.Collections);
        Assert.Same(records, _context.State.Collections[Environments].Records);
        Assert.Equal(2, records.Count);
        Assert.Equal("theirs", local["Dev"]);
    }

    //if the adapter does not show the pulled record locally, no state is written: otherwise the next sync would take
    //the record as deleted locally and delete it on the server
    [Fact]
    public async Task Execute_WhenPulledRecordDoesNotAppearLocally_ReportsFailedAndWritesNoState()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Server["Dev"] = new FakeContract("Dev", "d", 1);
        environments.IgnoresApplyLocal = true;
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        RegistrySyncReportItem dev = Assert.Single(result.Items);
        Assert.Equal(ERegistrySyncOutcome.Failed, dev.Outcome);
        Assert.Equal(nameof(RegistrySyncErrors.LocalRecordNotApplied), dev.Error?.Code);
        Assert.False(_context.State.Collections.ContainsKey(Environments));
        //ApplyLocal ran, so whatever it changed in the local data is saved
        _context.VerifySaved(Times.Once());
        RegistrySyncPlan nextPlan = await RegistrySyncTestContext.CreatePlan(sut);
        RegistrySyncPlanItem nextItem = Assert.Single(nextPlan.Items);
        Assert.Equal(ERegistrySyncAction.Pull, nextItem.Action);
        Assert.Equal(ERegistrySyncChange.Add, nextItem.Change);
    }

    [Fact]
    public async Task Execute_WhenSaveFails_ReportsThatChangesWereNotSaved()
    {
        // Arrange
        _context.ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "d";
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.True(result.Changed);
        Assert.False(result.Saved);
        _context.VerifySaved(Times.Once());
    }

    //the record is synced with version 3 and value "old"; then one or both sides change it
    private void ArrangeConflict(FakeRegistrySyncAdapter adapter, ERegistrySyncConflict conflict)
    {
        if (conflict != ERegistrySyncConflict.FirstSyncDiffers)
        {
            _context.AddSyncedRecord(adapter, "Dev", "old", 3);
        }

        if (conflict != ERegistrySyncConflict.DeletedLocally)
        {
            adapter.Local["Dev"] = "mine";
        }
        else
        {
            adapter.Local.Remove("Dev");
        }

        if (conflict != ERegistrySyncConflict.DeletedOnServer)
        {
            adapter.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        }
        else
        {
            adapter.Server.Remove("Dev");
        }
    }
}
