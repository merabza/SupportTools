using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//singleton (B5-ის GlobalSettings და ProjectCreatorSettings): ერთადერთი ჩანაწერი ფიქსირებული გასაღებით (სერვერის
//RecordName). ცარიელი კონტრაქტი (მისი ჰეში ახალი კონტრაქტის ჰეშს ემთხვევა) ორივე მხარეს არარსებულად ითვლება
//(მომხმარებლის გადაწყვეტილება, C3): ახალ კომპიუტერზე ჩანაწერი Pull-ით ჩამოდის და FirstSyncDiffers კონფლიქტს არ
//ქმნის. სერვერი singleton-ს არ შლის, ამიტომ:
//- სერვერიდან წაშლა ცარიელი კონტრაქტის ჩაწერაა, ლოკალური წაშლა კი საერთო ველების გასუფთავება;
//- სერვერზე დაცლილ ჩანაწერს ვერსია რჩება, ამიტომ ხელახალი შექმნა (მოსალოდნელი ვერსია 0) ამ ვერსიით სრულდება
public abstract class SingletonRegistrySyncAdapter<TContract> : RegistrySyncAdapter<TContract>
    where TContract : class, new()
{
    private readonly string _key;

    //სერვერზე შენახული ვერსია ბოლო წაკითხვისას; 0 — ჩანაწერი სერვერზე ჯერ არ შექმნილა
    private int _serverVersion;

    // ReSharper disable once ConvertToPrimaryConstructor
    protected SingletonRegistrySyncAdapter(string key, RegistrySyncWarnings warnings) : base(warnings)
    {
        _key = key;
    }

    public override async Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken)
    {
        Result<int> result = await WriteServerContract(new TContract(), expectedVersion, cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public override void RemoveLocal(string key)
    {
        ApplyContract(key, new TContract());
    }

    protected override IEnumerable<(string Key, TContract Contract)> GetLocalContracts()
    {
        TContract contract = GetLocalContract();
        if (IsEmpty(contract))
        {
            return [];
        }

        return [(_key, contract)];
    }

    protected override async Task<Result<List<TContract>>> GetServerContracts(CancellationToken cancellationToken)
    {
        Result<TContract> contract = await GetServerContract(cancellationToken);
        if (contract.IsFailure)
        {
            return Result.Failure<List<TContract>>(contract.Error);
        }

        _serverVersion = GetVersion(contract.Value);
        List<TContract> contracts = IsEmpty(contract.Value) ? [] : [contract.Value];
        return contracts;
    }

    protected override string GetKey(TContract contract)
    {
        return _key;
    }

    protected override Task<Result<int>> UpsertContract(string key, TContract contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        return WriteServerContract(contract, expectedVersion == 0 ? _serverVersion : expectedVersion,
            cancellationToken);
    }

    //ლოკალური ობიექტი კონტრაქტად; ობიექტი, რომელიც ჯერ არ არსებობს, ცარიელ კონტრაქტს იძლევა
    protected abstract TContract GetLocalContract();

    protected abstract Task<Result<TContract>> GetServerContract(CancellationToken cancellationToken);

    //upsert: version მოსალოდნელი ვერსიაა (0 — პირველი შექმნა); წარმატებისას ბრუნდება ახალი ვერსია
    protected abstract Task<Result<int>> WriteServerContract(TContract contract, int version,
        CancellationToken cancellationToken);

    private bool IsEmpty(TContract contract)
    {
        return RegistryContractHasher.ComputeHash(NormalizeContract(contract)) ==
               RegistryContractHasher.ComputeHash(NormalizeContract(new TContract()));
    }
}
