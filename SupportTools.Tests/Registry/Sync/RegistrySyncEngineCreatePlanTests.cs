using System;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

public sealed class RegistrySyncEngineCreatePlanTests
{
    private const string Environments = "Environments";

    private readonly RegistrySyncTestContext _context = new();

    [Fact]
    public async Task CreatePlan_WhenServerRecordsCannotBeRead_ReturnsTheServerErrorUnchanged()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "d";
        environments.ServerRecordsError = ApiClientErrors.ApiRequestFailed("http://sts/api/v1/environments: refused");
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        Result<RegistrySyncPlan> result = await sut.CreatePlan();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(RegistrySyncServerErrorCodes.RequestFailed, result.Error.Code);
    }

    //the collections are read in dependency order, so the error of the first failing collection is returned
    [Fact]
    public async Task CreatePlan_WhenSeveralCollectionsCannotBeRead_ReturnsTheErrorOfTheFirstCollectionInOrder()
    {
        // Arrange
        FakeRegistrySyncAdapter projects = _context.CreateAdapter("Projects", 2);
        projects.ServerRecordsError = Error.Problem("ProjectsFailed", "projects");
        FakeRegistrySyncAdapter runTimes = _context.CreateAdapter("RunTimes", 1);
        runTimes.ServerRecordsError = Error.Problem("RunTimesFailed", "runtimes");
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.ServerRecordsError = Error.Problem("EnvironmentsFailed", "environments");
        RegistrySyncEngine sut = _context.CreateEngine(projects, runTimes, environments);

        // Act
        Result<RegistrySyncPlan> result = await sut.CreatePlan();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("EnvironmentsFailed", result.Error.Code);
    }

    //G8: two local records that differ only by case cannot be matched with the server's records
    [Fact]
    public async Task CreatePlan_WhenLocalKeysDifferOnlyByCase_ReturnsDuplicateKeys()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "d";
        environments.Local["dev"] = "d2";
        environments.Local["Prod"] = "p";
        environments.Local["PROD"] = "p2";
        environments.Local["Test"] = "t";
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        Result<RegistrySyncPlan> result = await sut.CreatePlan();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(nameof(RegistrySyncErrors.DuplicateKeys), result.Error.Code);
        Assert.Equal("Environments: local keys differ only by case: Dev/dev, Prod/PROD", result.Error.Description);
    }

    [Fact]
    public async Task CreatePlan_WhenLocalNormalizationIsNotStable_ReturnsNormalizationIsNotStable()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "d";
        environments.HasUnstableNormalization = true;
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        Result<RegistrySyncPlan> result = await sut.CreatePlan();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(nameof(RegistrySyncErrors.NormalizationIsNotStable), result.Error.Code);
    }

    [Fact]
    public async Task CreatePlan_WhenServerNormalizationIsNotStable_ReturnsNormalizationIsNotStable()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Server["Prod"] = new FakeContract("Prod", "p", 1);
        environments.HasUnstableNormalization = true;
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        Result<RegistrySyncPlan> result = await sut.CreatePlan();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(nameof(RegistrySyncErrors.NormalizationIsNotStable), result.Error.Code);
        Assert.StartsWith("Environments/Prod:", result.Error.Description, StringComparison.Ordinal);
    }

    //"" locally and null on the server are the same after the adapter's normalization; the server's version is not
    //part of the content
    [Fact]
    public async Task CreatePlan_WhenContractsAreEqualAfterNormalization_FindsThemInSync()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "";
        environments.Server["Dev"] = new FakeContract("Dev", null, 7);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncPlan result = await RegistrySyncTestContext.CreatePlan(sut);

        // Assert
        RegistrySyncPlanItem item = Assert.Single(result.Items);
        Assert.Equal(ERegistrySyncAction.InSync, item.Action);
        Assert.Equal(new FakeContract("Dev", null, 0), item.Local?.Contract);
        Assert.Equal(7, item.Server?.Version);
    }

    [Fact]
    public async Task CreatePlan_WhenRecordsDiffer_ChangesNothing()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        _context.AddSyncedRecord(environments, "Dev", "d", 3);
        environments.Local["Dev"] = "changed";
        environments.Server["Prod"] = new FakeContract("Prod", "p", 1);
        RegistrySyncEngine sut = _context.CreateEngine(environments);

        // Act
        RegistrySyncPlan result = await RegistrySyncTestContext.CreatePlan(sut);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Empty(_context.Calls);
        Assert.Equal(3, _context.State.Collections[Environments].Records["Dev"].Version);
        Assert.Null(_context.State.LastSyncUtc);
        _context.VerifySaved(Times.Never());
    }

    //the public constructor stamps LastSyncUtc with the system clock
    [Fact]
    public async Task Constructor_WhenTimeProviderIsNotGiven_UsesTheSystemClock()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "d";
        var sut = new RegistrySyncEngine([environments], _context.ParametersManager.Object);
        DateTime before = DateTime.UtcNow;

        // Act
        await RegistrySyncTestContext.PlanAndExecute(sut, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.InRange(_context.State.LastSyncUtc ?? DateTime.MinValue, before, DateTime.UtcNow);
    }

    //the collection name is the key of the collection's sync state
    [Fact]
    public void Constructor_WhenCollectionNamesRepeat_Throws()
    {
        // Arrange
        FakeRegistrySyncAdapter first = _context.CreateAdapter(Environments, 1);
        FakeRegistrySyncAdapter second = _context.CreateAdapter("ENVIRONMENTS", 2);

        // Act
        Action act = () => _context.CreateEngine(first, second);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
