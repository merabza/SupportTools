using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using SystemTools.SharedKernel;

namespace SupportTools.Tests.Registry.Sync;

//a collection of name → value, like the simple collections of SupportToolsParameters. The local model is a case
//sensitive dictionary, as Newtonsoft creates it; the server keeps the records in memory, matches names case
//insensitively and follows the version rules of B1: version 0 creates, N updates or deletes only when the stored
//version is N (409 ConcurrencyConflict otherwise), a missing record answers 404 RecordWithNameNotFound
internal sealed class FakeRegistrySyncAdapter : IRegistrySyncAdapter
{
    private readonly List<string> _calls;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FakeRegistrySyncAdapter(string collectionName, int order, List<string> calls)
    {
        CollectionName = collectionName;
        Order = order;
        _calls = calls;
    }

    public Dictionary<string, string?> Local { get; } = [];
    public Dictionary<string, FakeContract> Server { get; } = new(StringComparer.OrdinalIgnoreCase);

    //key → the error that every server operation on this key answers
    public Dictionary<string, Error> ServerErrors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Error? ServerRecordsError { get; set; }

    //simulates an adapter bug: ApplyLocal leaves the local model unchanged
    public bool IgnoresApplyLocal { get; set; }

    //simulates an adapter bug: RemoveLocal leaves the local model unchanged
    public bool IgnoresRemoveLocal { get; set; }

    //simulates an adapter bug: every normalization changes the contract again
    public bool HasUnstableNormalization { get; set; }

    public string CollectionName { get; }
    public int Order { get; }

    public object Normalize(object contract)
    {
        var fakeContract = (FakeContract)contract;
        if (HasUnstableNormalization)
        {
            return fakeContract with { Value = fakeContract.Value + "!" };
        }

        return string.IsNullOrEmpty(fakeContract.Value) ? fakeContract with { Value = null } : fakeContract;
    }

    public IReadOnlyDictionary<string, object> GetLocalRecords()
    {
        return Local.ToDictionary(x => x.Key, object (x) => new FakeContract(x.Key, x.Value, 0));
    }

    public Task<Result<IReadOnlyDictionary<string, RegistryServerRecord>>> GetServerRecords(
        CancellationToken cancellationToken)
    {
        if (ServerRecordsError is not null)
        {
            return Task.FromResult(Result.Failure<IReadOnlyDictionary<string, RegistryServerRecord>>(ServerRecordsError));
        }

        IReadOnlyDictionary<string, RegistryServerRecord> records =
            Server.ToDictionary(x => x.Key, x => new RegistryServerRecord(x.Value, x.Value.Version));
        return Task.FromResult(Result.Success(records));
    }

    public Task<Result<int>> Upsert(string key, object contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        _calls.Add($"Upsert {CollectionName}/{key}/{expectedVersion}");
        if (ServerErrors.TryGetValue(key, out Error? error))
        {
            return Task.FromResult(Result.Failure<int>(error));
        }

        Server.TryGetValue(key, out FakeContract? stored);
        if (stored is null && expectedVersion > 0)
        {
            return Task.FromResult(Result.Failure<int>(RecordWithNameNotFound(key)));
        }

        if (stored is not null && stored.Version != expectedVersion)
        {
            return Task.FromResult(Result.Failure<int>(ConcurrencyConflict(key)));
        }

        int newVersion = expectedVersion + 1;
        Server[key] = (FakeContract)contract with { Name = key, Version = newVersion };
        return Task.FromResult(Result.Success(newVersion));
    }

    public Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        _calls.Add($"Delete {CollectionName}/{key}/{expectedVersion}");
        if (ServerErrors.TryGetValue(key, out Error? error))
        {
            return Task.FromResult(Result.Failure(error));
        }

        if (!Server.TryGetValue(key, out FakeContract? stored))
        {
            return Task.FromResult(Result.Failure(RecordWithNameNotFound(key)));
        }

        if (stored.Version != expectedVersion)
        {
            return Task.FromResult(Result.Failure(ConcurrencyConflict(key)));
        }

        Server.Remove(key);
        return Task.FromResult(Result.Success());
    }

    public void ApplyLocal(string key, object contract)
    {
        _calls.Add($"ApplyLocal {CollectionName}/{key}");
        if (!IgnoresApplyLocal)
        {
            Local[key] = ((FakeContract)contract).Value;
        }
    }

    public void RemoveLocal(string key)
    {
        _calls.Add($"RemoveLocal {CollectionName}/{key}");
        if (!IgnoresRemoveLocal)
        {
            Local.Remove(key);
        }
    }

    //the codes of B1's error factories, as ApiClient reads them from the ProblemDetails title
    public static Error ConcurrencyConflict(string key)
    {
        return Error.Conflict("ConcurrencyConflict", $"Record {key} was changed by someone else");
    }

    public static Error RecordWithNameNotFound(string key)
    {
        return Error.NotFound("RecordWithNameNotFound", $"Record With Name {key} Not Found");
    }
}
