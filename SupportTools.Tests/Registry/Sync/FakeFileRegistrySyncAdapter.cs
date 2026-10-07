using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using SystemTools.SharedKernel;

namespace SupportTools.Tests.Registry.Sync;

//a file collection (IRegistryFileSyncAdapter, C6) over a FakeRegistrySyncAdapter: MissingLocalKeys belong on this
//computer but are missing locally, and every PrepareApplyLocal is logged and answers its PrepareErrors entry
internal sealed class FakeFileRegistrySyncAdapter : IRegistryFileSyncAdapter
{
    private readonly List<string> _calls;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FakeFileRegistrySyncAdapter(FakeRegistrySyncAdapter inner, List<string> calls)
    {
        Inner = inner;
        _calls = calls;
    }

    //both sides of the collection
    public FakeRegistrySyncAdapter Inner { get; }

    public HashSet<string> MissingLocalKeys { get; } = new(StringComparer.OrdinalIgnoreCase);

    //key → the error that its preparation answers
    public Dictionary<string, Error> PrepareErrors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string CollectionName => Inner.CollectionName;
    public int Order => Inner.Order;

    public object Normalize(object contract)
    {
        return Inner.Normalize(contract);
    }

    public IReadOnlyDictionary<string, object> GetLocalRecords()
    {
        return Inner.GetLocalRecords();
    }

    public Task<Result<IReadOnlyDictionary<string, RegistryServerRecord>>> GetServerRecords(
        CancellationToken cancellationToken)
    {
        return Inner.GetServerRecords(cancellationToken);
    }

    public Task<Result<int>> Upsert(string key, object contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        return Inner.Upsert(key, contract, expectedVersion, cancellationToken);
    }

    public Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        return Inner.Delete(key, expectedVersion, cancellationToken);
    }

    public void ApplyLocal(string key, object contract)
    {
        Inner.ApplyLocal(key, contract);
    }

    public void RemoveLocal(string key)
    {
        Inner.RemoveLocal(key);
    }

    public IReadOnlyCollection<string> GetMissingLocalKeys()
    {
        return MissingLocalKeys;
    }

    public Task<Result> PrepareApplyLocal(string key, object contract, CancellationToken cancellationToken)
    {
        _calls.Add($"PrepareApplyLocal {CollectionName}/{key}");
        return Task.FromResult(PrepareErrors.TryGetValue(key, out Error? error)
            ? Result.Failure(error)
            : Result.Success());
    }
}
