using System.Collections.Generic;
using System.Linq;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsData.Models;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

public sealed class RegistrySyncPlannerTests
{
    private const string Environments = "Environments";
    private const string Dev = "Dev";

    //the table of README §4.3 and C2: a hash stands for the content, the server version for the server change.
    //Arguments: local hash, server hash and version, hash and version of the last successful sync (null = missing)
    [Theory]
    //on both sides and in the state
    [InlineData("A", "A", 1, "A", 1, "InSync")]
    [InlineData("A", "B", 2, "A", 1, "Pull(Update)")]
    [InlineData("B", "A", 1, "A", 1, "Push(Update)")]
    [InlineData("B", "C", 2, "A", 1, "Conflict(BothChanged)")]
    //both changed to the same content: nothing to transfer, only the state is updated
    [InlineData("B", "B", 2, "A", 1, "InSync")]
    //one side changed, but the content is already equal: nothing to transfer either
    [InlineData("A", "A", 2, "A", 1, "InSync")]
    [InlineData("B", "B", 1, "A", 1, "InSync")]
    //neither side changed since the last sync: the state is trusted
    [InlineData("A", "B", 1, "A", 1, "InSync")]
    //only local: deleted on the server or new
    [InlineData("A", null, null, "A", 1, "Pull(Delete)")]
    [InlineData("B", null, null, "A", 1, "Conflict(DeletedOnServer)")]
    [InlineData("A", null, null, null, null, "Push(Add)")]
    //only on the server: deleted locally or new
    [InlineData(null, "A", 1, "A", 1, "Push(Delete)")]
    [InlineData(null, "B", 2, "A", 1, "Conflict(DeletedLocally)")]
    [InlineData(null, "A", 1, null, null, "Pull(Add)")]
    //on both sides, but not in the state: first sync, or added on both sides
    [InlineData("A", "A", 3, null, null, "InSync")]
    [InlineData("A", "B", 3, null, null, "Conflict(FirstSyncDiffers)")]
    //only in the state: deleted on both sides
    [InlineData(null, null, null, "A", 1, "InSync")]
    public void CreatePlan_ForRecord_DecidesAction(string? localHash, string? serverHash, int? serverVersion,
        string? stateHash, int? stateVersion, string expected)
    {
        // Arrange
        RegistrySyncStateModel state = CreateState(stateHash, stateVersion);
        RegistryCollectionSnapshot collection = CreateCollection(localHash, serverHash, serverVersion);

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        RegistrySyncPlanItem item = Assert.Single(result.Items);
        Assert.Equal(expected, Describe(item));
        Assert.Equal(Environments, item.CollectionName);
        Assert.Equal(Dev, item.Key);
        Assert.Equal(localHash, item.Local?.Hash);
        Assert.Equal(serverHash, item.Server?.Hash);
    }

    [Theory]
    [InlineData("B", "C", 2, "A", 1)]
    [InlineData("A", null, null, null, null)]
    [InlineData(null, "A", 1, null, null)]
    [InlineData(null, null, null, "A", 1)]
    public void CreatePlan_WhenKeyIsExcluded_SkipsRecord(string? localHash, string? serverHash, int? serverVersion,
        string? stateHash, int? stateVersion)
    {
        // Arrange
        RegistrySyncStateModel state = CreateState(stateHash, stateVersion);
        state.GetOrAddCollection(Environments).ExcludedKeys.Add("DEV");
        RegistryCollectionSnapshot collection = CreateCollection(localHash, serverHash, serverVersion);

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        RegistrySyncPlanItem item = Assert.Single(result.Items);
        Assert.Equal("Skipped", Describe(item));
    }

    //a file collection (C6): a delete in either direction waits for the user's choice, so it is a conflict; the other
    //decisions are those of the table
    [Theory]
    [InlineData("A", null, null, "A", 1, "Conflict(DeleteNeedsConfirmation)")]
    [InlineData(null, "A", 1, "A", 1, "Conflict(DeleteNeedsConfirmation)")]
    [InlineData("B", null, null, "A", 1, "Conflict(DeletedOnServer)")]
    [InlineData(null, "B", 2, "A", 1, "Conflict(DeletedLocally)")]
    [InlineData("A", null, null, null, null, "Push(Add)")]
    [InlineData(null, "A", 1, null, null, "Pull(Add)")]
    [InlineData("A", "B", 2, "A", 1, "Pull(Update)")]
    [InlineData("B", "A", 1, "A", 1, "Push(Update)")]
    [InlineData(null, null, null, "A", 1, "InSync")]
    public void CreatePlan_WhenDeletesNeedConfirmation_TurnsEveryDeleteIntoAConflict(string? localHash,
        string? serverHash, int? serverVersion, string? stateHash, int? stateVersion, string expected)
    {
        // Arrange
        RegistrySyncStateModel state = CreateState(stateHash, stateVersion);
        RegistryCollectionSnapshot collection = CreateCollection(localHash, serverHash, serverVersion) with
        {
            DeletesNeedConfirmation = true
        };

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        Assert.Equal(expected, Describe(Assert.Single(result.Items)));
    }

    //a record that belongs on this computer but is missing locally (a file that the registry points to) was not
    //deleted here: it is taken from the server, whatever the state says, and never deleted on the server
    [Theory]
    [InlineData("A", 1, "A", 1)]
    [InlineData("B", 2, "A", 1)]
    [InlineData("A", 1, null, null)]
    public void CreatePlan_WhenServerRecordIsMissingLocally_PullsIt(string serverHash, int serverVersion,
        string? stateHash, int? stateVersion)
    {
        // Arrange
        RegistrySyncStateModel state = CreateState(stateHash, stateVersion);
        RegistryCollectionSnapshot collection = CreateCollection(null, serverHash, serverVersion) with
        {
            DeletesNeedConfirmation = true, MissingLocalKeys = ["DEV"]
        };

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        RegistrySyncPlanItem item = Assert.Single(result.Items);
        Assert.Equal("Pull(Add)", Describe(item));
        Assert.Equal(Dev, item.Key);
    }

    //missing on both sides: nothing to take, the state is forgotten; an excluded key stays excluded
    [Theory]
    [InlineData(null, null, false, "InSync")]
    [InlineData("A", 1, true, "Skipped")]
    public void CreatePlan_WhenMissingRecordIsNotTaken_DoesNotPullIt(string? serverHash, int? serverVersion,
        bool isExcluded, string expected)
    {
        // Arrange
        RegistrySyncStateModel state = CreateState("A", 1);
        if (isExcluded)
        {
            state.GetOrAddCollection(Environments).ExcludedKeys.Add(Dev);
        }

        RegistryCollectionSnapshot collection = CreateCollection(null, serverHash, serverVersion) with
        {
            MissingLocalKeys = [Dev]
        };

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        Assert.Equal(expected, Describe(Assert.Single(result.Items)));
    }

    //G8: one record, whatever case each side uses; the plan shows the local spelling
    [Fact]
    public void CreatePlan_WhenKeysDifferOnlyByCase_MatchesThemAsOneRecord()
    {
        // Arrange
        var state = new RegistrySyncStateModel();
        state.GetOrAddCollection(Environments).Records["DEV"] =
            new RegistryRecordSyncStateModel { Version = 1, Hash = "A" };
        var collection = new RegistryCollectionSnapshot(Environments, 1,
            new Dictionary<string, RegistrySyncRecord> { ["dev"] = new("local", "A", 0) },
            new Dictionary<string, RegistrySyncRecord> { ["Dev"] = new("server", "B", 2) });

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        RegistrySyncPlanItem item = Assert.Single(result.Items);
        Assert.Equal("dev", item.Key);
        Assert.Equal("Pull(Update)", Describe(item));
    }

    [Fact]
    public void CreatePlan_WhenRecordIsNotLocal_UsesServerSpellingThenStateSpelling()
    {
        // Arrange
        var state = new RegistrySyncStateModel();
        RegistryCollectionSyncStateModel collectionState = state.GetOrAddCollection(Environments);
        collectionState.Records["PROD"] = new RegistryRecordSyncStateModel { Version = 1, Hash = "A" };
        collectionState.Records["TEST"] = new RegistryRecordSyncStateModel { Version = 1, Hash = "A" };
        var collection = new RegistryCollectionSnapshot(Environments, 1, new Dictionary<string, RegistrySyncRecord>(),
            new Dictionary<string, RegistrySyncRecord> { ["Prod"] = new("server", "A", 1) });

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([collection], state);

        // Assert
        Assert.Collection(result.Items, x => Assert.Equal("Prod", x.Key), x => Assert.Equal("TEST", x.Key));
    }

    [Fact]
    public void CreatePlan_WhenThereAreSeveralCollections_OrdersItemsByOrderThenCollectionThenKey()
    {
        // Arrange
        var projects = new RegistryCollectionSnapshot("Projects", 9,
            new Dictionary<string, RegistrySyncRecord> { ["App"] = new("local", "A", 0) },
            new Dictionary<string, RegistrySyncRecord>());
        var runTimes = new RegistryCollectionSnapshot("RunTimes", 1,
            new Dictionary<string, RegistrySyncRecord> { ["net"] = new("local", "A", 0) },
            new Dictionary<string, RegistrySyncRecord>());
        var environments = new RegistryCollectionSnapshot(Environments, 1,
            new Dictionary<string, RegistrySyncRecord> { ["prod"] = new("local", "A", 0) },
            new Dictionary<string, RegistrySyncRecord> { ["Dev"] = new("server", "A", 1) });

        // Act
        RegistrySyncPlan result = RegistrySyncPlanner.CreatePlan([projects, runTimes, environments],
            new RegistrySyncStateModel());

        // Assert
        string[] expected = ["Environments/Dev", "Environments/prod", "RunTimes/net", "Projects/App"];
        Assert.Equal(expected, result.Items.Select(x => $"{x.CollectionName}/{x.Key}"));
        Assert.All(result.Items.Where(x => x.CollectionName == "Projects"), x => Assert.Equal(9, x.Order));
    }

    //the planner is a pure function: it neither adds collections nor changes records of the state
    [Fact]
    public void CreatePlan_WhenStateIsUsed_DoesNotChangeIt()
    {
        // Arrange
        RegistrySyncStateModel state = CreateState("A", 1);
        var otherCollection = new RegistryCollectionSnapshot("RunTimes", 2,
            new Dictionary<string, RegistrySyncRecord> { ["net"] = new("local", "A", 0) },
            new Dictionary<string, RegistrySyncRecord>());

        // Act
        RegistrySyncPlanner.CreatePlan([CreateCollection("B", "B", 2), otherCollection], state);

        // Assert
        KeyValuePair<string, RegistryCollectionSyncStateModel> collection = Assert.Single(state.Collections);
        Assert.Equal(Environments, collection.Key);
        RegistryRecordSyncStateModel record = Assert.Single(collection.Value.Records).Value;
        Assert.Equal(1, record.Version);
        Assert.Equal("A", record.Hash);
    }

    private static RegistrySyncStateModel CreateState(string? stateHash, int? stateVersion)
    {
        var state = new RegistrySyncStateModel();
        if (stateHash is not null)
        {
            state.GetOrAddCollection(Environments).Records[Dev] = new RegistryRecordSyncStateModel
            {
                Version = stateVersion ?? 0, Hash = stateHash
            };
        }

        return state;
    }

    private static RegistryCollectionSnapshot CreateCollection(string? localHash, string? serverHash,
        int? serverVersion)
    {
        var localRecords = new Dictionary<string, RegistrySyncRecord>();
        if (localHash is not null)
        {
            localRecords[Dev] = new RegistrySyncRecord("local contract", localHash, 0);
        }

        var serverRecords = new Dictionary<string, RegistrySyncRecord>();
        if (serverHash is not null)
        {
            serverRecords[Dev] = new RegistrySyncRecord("server contract", serverHash, serverVersion ?? 0);
        }

        return new RegistryCollectionSnapshot(Environments, 1, localRecords, serverRecords);
    }

    //the notation of the task: Action, with the kind of change or conflict when there is one
    private static string Describe(RegistrySyncPlanItem item)
    {
        string description = item.Action.ToString();
        if (item.Change != ERegistrySyncChange.None)
        {
            description += $"({item.Change})";
        }

        if (item.Conflict != ERegistrySyncConflict.None)
        {
            description += $"({item.Conflict})";
        }

        return description;
    }
}
