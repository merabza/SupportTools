using System.Collections.Generic;
using LibSupportToolsServerWork.Registry.Sync;
using SystemTools.ApiContracts.Errors;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

//the local changes since the last sync, read without the server (D2 skips the push when there are none). Every
//server read of these tests fails, so a test that passes has not asked the server
public sealed class RegistrySyncEngineGetLocalChangesTests
{
    private const string Environments = "Environments";
    private const string StoredFiles = "StoredFiles";

    private readonly RegistrySyncTestContext _context = new();
    private readonly FakeRegistrySyncAdapter _environments;

    public RegistrySyncEngineGetLocalChangesTests()
    {
        _environments = CreateAdapter(Environments, 1);
    }

    [Fact]
    public void GetLocalChanges_WhenEveryRecordIsAsLastSynced_ReturnsNothing()
    {
        // Arrange
        _context.AddSyncedRecord(_environments, "Dev", "d", 3);
        _context.AddSyncedRecord(_environments, "Prod", "p", 1);
        RegistrySyncEngine sut = _context.CreateEngine(_environments);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    //a changed record and a new one carry their new hash, a record deleted here carries none; ordered by key
    [Fact]
    public void GetLocalChanges_WhenRecordsChangedWereAddedOrDeleted_ReturnsThemByKey()
    {
        // Arrange
        _context.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "new";
        _context.AddSyncedRecord(_environments, "Gone", "g", 2);
        _environments.Local.Remove("Gone");
        _environments.Local["Added"] = "a";
        _context.AddSyncedRecord(_environments, "Same", "s", 1);
        RegistrySyncEngine sut = _context.CreateEngine(_environments);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.Equal(
        [
            new RegistrySyncLocalChange(Environments, "Added", RegistrySyncTestContext.HashOf("Added", "a")),
            new RegistrySyncLocalChange(Environments, "Dev", RegistrySyncTestContext.HashOf("Dev", "new")),
            new RegistrySyncLocalChange(Environments, "Gone", null)
        ], result.Value);
    }

    //the hash is that of the normalized contract, as in the plan: "" and null are the same value
    [Fact]
    public void GetLocalChanges_WhenRecordDiffersOnlyBeforeNormalization_ReturnsNothing()
    {
        // Arrange
        _context.State.GetOrAddCollection(Environments).Records["Dev"] = new()
        {
            Version = 1, Hash = RegistryContractHasher.ComputeHash(new FakeContract("Dev", null, 0))
        };
        _environments.Local["Dev"] = string.Empty;
        RegistrySyncEngine sut = _context.CreateEngine(_environments);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.Empty(result.Value);
    }

    [Fact]
    public void GetLocalChanges_WhenChangedRecordsAreExcluded_ReturnsNothing()
    {
        // Arrange
        _context.AddSyncedRecord(_environments, "Dev", "old", 3);
        _environments.Local["Dev"] = "new";
        _context.AddSyncedRecord(_environments, "Gone", "g", 2);
        _environments.Local.Remove("Gone");
        _environments.Local["Linux"] = "l";
        _context.State.GetOrAddCollection(Environments).ExcludedKeys.UnionWith(["dev", "GONE", "Linux"]);
        RegistrySyncEngine sut = _context.CreateEngine(_environments);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.Empty(result.Value);
    }

    //a missing file of a file collection is taken from the server again (C6), so it is no local deletion
    [Fact]
    public void GetLocalChanges_WhenFileIsMissingLocally_DoesNotReportItAsDeleted()
    {
        // Arrange
        FakeFileRegistrySyncAdapter storedFiles = _context.CreateFileAdapter(StoredFiles, 2);
        storedFiles.Inner.ServerRecordsError = ServerIsUnreachable();
        _context.AddSyncedRecord(storedFiles.Inner, @"D:\Secrets\a.json", "a", 1);
        storedFiles.Inner.Local.Remove(@"D:\Secrets\a.json");
        storedFiles.MissingLocalKeys.Add(@"d:\secrets\A.json");
        _context.AddSyncedRecord(storedFiles.Inner, @"D:\Secrets\b.json", "b", 1);
        storedFiles.Inner.Local.Remove(@"D:\Secrets\b.json");
        RegistrySyncEngine sut = _context.CreateEngine(_environments, storedFiles);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.Equal(new RegistrySyncLocalChange(StoredFiles, @"D:\Secrets\b.json", null), Assert.Single(result.Value));
    }

    [Fact]
    public void GetLocalChanges_WhenLocalKeysDifferOnlyByCase_ReturnsTheErrorOfThePlan()
    {
        // Arrange
        _environments.Local["Dev"] = "d";
        _environments.Local["DEV"] = "D";
        RegistrySyncEngine sut = _context.CreateEngine(_environments);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(nameof(RegistrySyncErrors.DuplicateKeys), result.Error.Code);
    }

    [Fact]
    public void GetLocalChanges_WhenCollectionsHaveChanges_ListsThemInDependencyOrder()
    {
        // Arrange
        FakeRegistrySyncAdapter projects = CreateAdapter("Projects", 3);
        projects.Local["App"] = "a";
        _environments.Local["Dev"] = "d";
        RegistrySyncEngine sut = _context.CreateEngine(projects, _environments);

        // Act
        Result<List<RegistrySyncLocalChange>> result = sut.GetLocalChanges();

        // Assert
        Assert.Equal([Environments, "Projects"], result.Value.ConvertAll(x => x.CollectionName));
    }

    private FakeRegistrySyncAdapter CreateAdapter(string collectionName, int order)
    {
        FakeRegistrySyncAdapter adapter = _context.CreateAdapter(collectionName, order);
        adapter.ServerRecordsError = ServerIsUnreachable();
        return adapter;
    }

    private static Error ServerIsUnreachable()
    {
        return ApiClientErrors.ApiRequestFailed("http://sts/api/v1: refused");
    }
}
