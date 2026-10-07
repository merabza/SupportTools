using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

//a file collection (IRegistryFileSyncAdapter, C6) through the engine: its missing records are taken from the server,
//its deletes wait for the user's choice, and its pulled records are prepared (the content downloaded) before ApplyLocal
public sealed class RegistrySyncEngineFileAdapterTests
{
    private const string StoredFiles = "StoredFiles";
    private const string Projects = "Projects";

    private readonly RegistrySyncTestContext _context = new();
    private readonly FakeFileRegistrySyncAdapter _files;

    public RegistrySyncEngineFileAdapterTests()
    {
        _files = _context.CreateFileAdapter(StoredFiles, 165);
    }

    //a synced file that is missing here but still belongs here is taken again instead of being deleted on the server;
    //a file deleted on one side waits for the user instead of being deleted on the other side
    [Fact]
    public async Task CreatePlan_WhenAdapterHandlesFiles_TakesMissingFilesAndAsksBeforeDeletes()
    {
        // Arrange
        _context.AddSyncedRecord(_files.Inner, "Missing", "m", 2);
        _files.Inner.Local.Remove("Missing");
        _files.MissingLocalKeys.Add("Missing");
        ArrangeDeletedOnEachSide();
        RegistrySyncEngine sut = _context.CreateEngine(_files);

        // Act
        RegistrySyncPlan result = await RegistrySyncTestContext.CreatePlan(sut);

        // Assert
        string[] expected =
        [
            "GoneHere Conflict None DeleteNeedsConfirmation", "GoneOnServer Conflict None DeleteNeedsConfirmation",
            "Missing Pull Add None"
        ];
        Assert.Equal(expected, result.Items.Select(x => $"{x.Key} {x.Action} {x.Change} {x.Conflict}"));
    }

    //a collection without the file capability keeps its plain deletes
    [Fact]
    public async Task CreatePlan_WhenAdapterDoesNotHandleFiles_KeepsItsDeletes()
    {
        // Arrange
        FakeRegistrySyncAdapter projects = _context.CreateAdapter(Projects, 170);
        _context.AddSyncedRecord(projects, "AppGone", "p", 3);
        projects.Server.Remove("AppGone");
        RegistrySyncEngine sut = _context.CreateEngine(_files, projects);

        // Act
        RegistrySyncPlan result = await RegistrySyncTestContext.CreatePlan(sut);

        // Assert
        RegistrySyncPlanItem item = RegistrySyncTestContext.PlanItem(result, Projects, "AppGone");
        Assert.Equal((ERegistrySyncAction.Pull, ERegistrySyncChange.Delete), (item.Action, item.Change));
    }

    [Fact]
    public async Task Execute_WhenFileIsPulled_PreparesItBeforeApplyLocalAndStoresItsState()
    {
        // Arrange
        _files.Inner.Server["New"] = new FakeContract("New", "n", 1);
        RegistrySyncEngine sut = _context.CreateEngine(_files);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal(["PrepareApplyLocal StoredFiles/New", "ApplyLocal StoredFiles/New"], _context.Calls);
        Assert.Equal(ERegistrySyncOutcome.Done, RegistrySyncTestContext.ReportItem(result, StoredFiles, "New").Outcome);
        RegistryRecordSyncStateModel state = _context.State.Collections[StoredFiles].Records["New"];
        Assert.Equal((1, RegistrySyncTestContext.HashOf("New", "n")), (state.Version, state.Hash));
    }

    //a file whose content cannot be taken is not applied and gets no state; the other files are applied
    [Fact]
    public async Task Execute_WhenPreparationFails_ReportsFailedWithoutApplyingOrState()
    {
        // Arrange
        _files.Inner.Server["Broken"] = new FakeContract("Broken", "b", 1);
        _files.Inner.Server["Good"] = new FakeContract("Good", "g", 1);
        Error error = Error.Problem("RecordWithNameNotFound", "Record With Name Broken Not Found");
        _files.PrepareErrors["Broken"] = error;
        RegistrySyncEngine sut = _context.CreateEngine(_files);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        RegistrySyncReportItem broken = RegistrySyncTestContext.ReportItem(result, StoredFiles, "Broken");
        Assert.Equal(ERegistrySyncOutcome.Failed, broken.Outcome);
        Assert.Same(error, broken.Error);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, StoredFiles, "Good").Outcome);
        Assert.DoesNotContain("ApplyLocal StoredFiles/Broken", _context.Calls);
        Assert.False(_context.State.Collections[StoredFiles].Records.ContainsKey("Broken"));
        Assert.Null(result.TransportError);
        Assert.True(result.Saved);
    }

    //the content comes from the server: after a transport error the remaining files are not taken, while the other
    //collections still apply their pulls
    [Fact]
    public async Task Execute_WhenServerBecomesUnreachableDuringPreparation_DoesNotPrepareTheRemainingFiles()
    {
        // Arrange
        FakeRegistrySyncAdapter projects = _context.CreateAdapter(Projects, 170);
        projects.Server["AppFake"] = new FakeContract("AppFake", "p", 1);
        _files.Inner.Server["A"] = new FakeContract("A", "a", 1);
        _files.Inner.Server["B"] = new FakeContract("B", "b", 1);
        Error transportError = ApiClientErrors.ApiRequestFailed("http://127.0.0.1:0/api/v1/files/content: refused");
        _files.PrepareErrors["A"] = transportError;
        RegistrySyncEngine sut = _context.CreateEngine(_files, projects);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal(["PrepareApplyLocal StoredFiles/A", "ApplyLocal Projects/AppFake"], _context.Calls);
        Assert.Equal(ERegistrySyncOutcome.Failed, RegistrySyncTestContext.ReportItem(result, StoredFiles, "A").Outcome);
        Assert.Equal(ERegistrySyncOutcome.NotExecuted,
            RegistrySyncTestContext.ReportItem(result, StoredFiles, "B").Outcome);
        Assert.Equal(ERegistrySyncOutcome.Done,
            RegistrySyncTestContext.ReportItem(result, Projects, "AppFake").Outcome);
        Assert.Same(transportError, result.TransportError);
    }

    [Fact]
    public async Task Execute_WhenServerWasUnreachableForServerOperations_DoesNotPrepareFiles()
    {
        // Arrange
        FakeRegistrySyncAdapter projects = _context.CreateAdapter(Projects, 170);
        projects.Local["AppFake"] = "p";
        projects.ServerErrors["AppFake"] =
            ApiClientErrors.ApiRequestFailed("http://127.0.0.1:0/api/v1/projects/update/AppFake: refused");
        _files.Inner.Server["A"] = new FakeContract("A", "a", 1);
        RegistrySyncEngine sut = _context.CreateEngine(_files, projects);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal(["Upsert Projects/AppFake/0"], _context.Calls);
        Assert.Equal(ERegistrySyncOutcome.NotExecuted,
            RegistrySyncTestContext.ReportItem(result, StoredFiles, "A").Outcome);
    }

    //Apply and the other presets never delete a file: the delete conflicts stay until the user resolves them
    [Fact]
    public async Task Execute_WhenDeleteConflictsAreNotResolved_DeletesNothing()
    {
        // Arrange
        ArrangeDeletedOnEachSide();
        RegistrySyncEngine sut = _context.CreateEngine(_files);

        // Act
        RegistrySyncReport result =
            await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Empty(_context.Calls);
        Assert.All(result.Items, x => Assert.Equal(ERegistrySyncOutcome.NotSelected, x.Outcome));
        Assert.True(_files.Inner.Local.ContainsKey("GoneOnServer"));
        Assert.True(_files.Inner.Server.ContainsKey("GoneHere"));
    }

    //the user's choice decides: the side where the file is gone wins (the file is deleted on the other side too) or
    //loses (the file comes back)
    [Theory]
    [InlineData("GoneOnServer", ERegistryConflictResolution.Server, "RemoveLocal StoredFiles/GoneOnServer")]
    [InlineData("GoneOnServer", ERegistryConflictResolution.Local, "Upsert StoredFiles/GoneOnServer/0")]
    [InlineData("GoneHere", ERegistryConflictResolution.Local, "Delete StoredFiles/GoneHere/4")]
    [InlineData("GoneHere", ERegistryConflictResolution.Server,
        "PrepareApplyLocal StoredFiles/GoneHere|ApplyLocal StoredFiles/GoneHere")]
    public async Task Execute_WhenDeleteConflictIsResolved_RunsTheChosenSide(string key,
        ERegistryConflictResolution resolution, string expectedCalls)
    {
        // Arrange
        ArrangeDeletedOnEachSide();
        RegistrySyncEngine sut = _context.CreateEngine(_files);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(sut);
        var selection = new RegistrySyncSelection
        {
            ConflictResolutions = new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution>
            {
                [RegistrySyncTestContext.PlanItem(plan, StoredFiles, key)] = resolution
            }
        };

        // Act
        RegistrySyncReport result = await sut.Execute(plan, selection);

        // Assert
        Assert.Equal(expectedCalls.Split('|'), _context.Calls);
        Assert.Equal(ERegistrySyncOutcome.Done, RegistrySyncTestContext.ReportItem(result, StoredFiles, key).Outcome);
    }

    //GoneOnServer: deleted on the server, unchanged here; GoneHere: deleted here, unchanged on the server
    private void ArrangeDeletedOnEachSide()
    {
        _context.AddSyncedRecord(_files.Inner, "GoneOnServer", "g", 3);
        _files.Inner.Server.Remove("GoneOnServer");
        _context.AddSyncedRecord(_files.Inner, "GoneHere", "h", 4);
        _files.Inner.Local.Remove("GoneHere");
    }
}
