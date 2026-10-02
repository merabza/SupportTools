using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using Moq;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SharedKernel;
using Xunit;

namespace SupportTools.Tests.Registry.Sync;

//the parameters with their sync state, a fake IParametersManager and the call log that the fake adapters share
internal sealed class RegistrySyncTestContext
{
    public static readonly DateTimeOffset Now = new(2026, 10, 2, 12, 30, 0, TimeSpan.Zero);

    public RegistrySyncTestContext()
    {
        ParametersManager.SetupGet(x => x.Parameters).Returns(Parameters);
        ParametersManager.Setup(x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    public SupportToolsParameters Parameters { get; } = new();
    public Mock<IParametersManager> ParametersManager { get; } = new();
    public List<string> Calls { get; } = [];
    public RegistrySyncStateModel State => Parameters.RegistrySyncState;

    public FakeRegistrySyncAdapter CreateAdapter(string collectionName, int order)
    {
        return new FakeRegistrySyncAdapter(collectionName, order, Calls);
    }

    public RegistrySyncEngine CreateEngine(params FakeRegistrySyncAdapter[] adapters)
    {
        return new RegistrySyncEngine(adapters, ParametersManager.Object, new FixedTimeProvider(Now));
    }

    //a record that the last sync left equal on both sides
    public void AddSyncedRecord(FakeRegistrySyncAdapter adapter, string key, string value, int version)
    {
        adapter.Local[key] = value;
        adapter.Server[key] = new FakeContract(key, value, version);
        SetState(adapter.CollectionName, key, value, version);
    }

    public void SetState(string collectionName, string key, string value, int version)
    {
        State.GetOrAddCollection(collectionName).Records[key] = new RegistryRecordSyncStateModel
        {
            Version = version, Hash = HashOf(key, value)
        };
    }

    //the hash of a local record of the fake adapter
    public static string HashOf(string key, string value)
    {
        return RegistryContractHasher.ComputeHash(new FakeContract(key, value, 0));
    }

    public static async Task<RegistrySyncPlan> CreatePlan(RegistrySyncEngine engine)
    {
        Result<RegistrySyncPlan> plan = await engine.CreatePlan();
        Assert.True(plan.IsSuccess, plan.IsFailure ? plan.Error.Description : null);
        return plan.Value;
    }

    public static async Task<RegistrySyncReport> PlanAndExecute(RegistrySyncEngine engine,
        RegistrySyncSelection selection)
    {
        RegistrySyncPlan plan = await CreatePlan(engine);
        return await engine.Execute(plan, selection);
    }

    public static RegistrySyncPlanItem PlanItem(RegistrySyncPlan plan, string collectionName, string key)
    {
        return plan.Items.Single(x => x.CollectionName == collectionName && x.Key == key);
    }

    public static RegistrySyncReportItem ReportItem(RegistrySyncReport report, string collectionName, string key)
    {
        return report.Items.Single(x => x.PlanItem.CollectionName == collectionName && x.PlanItem.Key == key);
    }

    public void VerifySaved(Times times)
    {
        ParametersManager.Verify(
            x => x.Save(It.IsAny<IParameters>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), times);
    }
}
