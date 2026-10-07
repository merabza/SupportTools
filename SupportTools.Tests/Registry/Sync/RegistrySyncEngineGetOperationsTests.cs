using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

//GetOperations shows what Execute would do (the Dry run of C5) and changes nothing
public sealed class RegistrySyncEngineGetOperationsTests
{
    private const string Environments = "Environments";
    private const string Servers = "Servers";

    private readonly RegistrySyncTestContext _context = new();

    [Fact]
    public async Task GetOperations_WhenAllKindsAreSelected_ListsThemInExecutionOrderWithoutRunningThem()
    {
        // Arrange
        FakeRegistrySyncAdapter servers = _context.CreateAdapter(Servers, 2);
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        List<FakeRegistrySyncAdapter> adapters = [servers, environments];
        foreach (FakeRegistrySyncAdapter adapter in adapters)
        {
            adapter.Local["PushAdd"] = "new";
            adapter.Server["PullAdd"] = new FakeContract("PullAdd", "new", 1);
            adapter.Server["PushDelete"] = new FakeContract("PushDelete", "old", 4);
            _context.SetState(adapter.CollectionName, "PushDelete", "old", 4);
            adapter.Local["PullDelete"] = "old";
            _context.SetState(adapter.CollectionName, "PullDelete", "old", 2);
        }

        RegistrySyncEngine engine = _context.CreateEngine(servers, environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(engine);

        // Act
        RegistrySyncOperations result = RegistrySyncEngine.GetOperations(plan, RegistrySyncSelection.AllNonConflicting);

        // Assert
        Assert.Equal(
            ["Environments/PushAdd", "Servers/PushAdd", "Servers/PushDelete", "Environments/PushDelete"],
            Names(result.ServerItems));
        Assert.Equal(
            ["Environments/PullAdd", "Servers/PullAdd", "Servers/PullDelete", "Environments/PullDelete"],
            Names(result.LocalItems));
        Assert.Empty(_context.Calls);
        _context.VerifySaved(Times.Never());
    }

    [Fact]
    public async Task GetOperations_WhenOnlyPullsAreSelected_ListsNoServerOperation()
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Mine"] = "m";
        environments.Server["Theirs"] = new FakeContract("Theirs", "t", 1);
        RegistrySyncEngine engine = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(engine);

        // Act
        RegistrySyncOperations result = RegistrySyncEngine.GetOperations(plan, RegistrySyncSelection.PullOnly);

        // Assert
        Assert.Empty(result.ServerItems);
        Assert.Equal(["Environments/Theirs"], Names(result.LocalItems));
    }

    //a conflict goes to the side its resolution names; an unresolved or skipped conflict goes nowhere
    [Theory]
    [InlineData(ERegistryConflictResolution.Local, 1, 0)]
    [InlineData(ERegistryConflictResolution.Server, 0, 1)]
    [InlineData(ERegistryConflictResolution.Skip, 0, 0)]
    public async Task GetOperations_WhenConflictIsResolved_PutsItOnTheChosenSide(
        ERegistryConflictResolution resolution, int expectedServerCount, int expectedLocalCount)
    {
        // Arrange
        FakeRegistrySyncAdapter environments = _context.CreateAdapter(Environments, 1);
        environments.Local["Dev"] = "mine";
        environments.Server["Dev"] = new FakeContract("Dev", "theirs", 4);
        RegistrySyncEngine engine = _context.CreateEngine(environments);
        RegistrySyncPlan plan = await RegistrySyncTestContext.CreatePlan(engine);
        var selection = new RegistrySyncSelection
        {
            ConflictResolutions =
                new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution> { [plan.Items[0]] = resolution }
        };

        // Act
        RegistrySyncOperations result = RegistrySyncEngine.GetOperations(plan, selection);

        // Assert
        Assert.Equal(expectedServerCount, result.ServerItems.Count);
        Assert.Equal(expectedLocalCount, result.LocalItems.Count);
    }

    private static IEnumerable<string> Names(IEnumerable<RegistrySyncPlanItem> items)
    {
        return items.Select(x => $"{x.CollectionName}/{x.Key}");
    }
}
