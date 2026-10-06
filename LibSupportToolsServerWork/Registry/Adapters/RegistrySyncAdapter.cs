using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibSupportToolsServerWork.Registry.Sync;
using SupportToolsServerApiContracts.Errors;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Adapters;

//რეესტრის ერთი კოლექციის ადაპტერის საერთო ნაწილი (C3; C4 მასზე Projects-ს აშენებს): IRegistrySyncAdapter-ის object
//კონტრაქტები აქ TContract-ად (სერვერის Sts…DataModel) გარდაიქმნება. ლოკალური და სერვერის ჩანაწერები რეგისტრზე
//მგრძნობიარე dictionary-ებშია, რომ მხოლოდ რეგისტრით განსხვავებული გასაღებები ძრავამ შეცდომად დააბრუნოს (G8).
//ჩანაწერი ორივე მხარეს გამოირიცხება (გეგმაში არ ჩანს: არც Push, არც Pull, არც წაშლა), თუ:
//- IsSynced მას უარყოფს (მაგ. bootstrap ApiClient, შაბლონი, რომლის ფაილიც აკლია);
//- სერვერის კონტრაქტში ამ კლიენტისთვის უცნობი მნიშვნელობაა (FindUnsupportedField, მაგ. enum-ის ახალი სახელი). ასეთი
//  ჩანაწერი ლოკალურად ვერ აისახება, ლოკალური ვერსიის ატვირთვა კი სერვერის მნიშვნელობას დაკარგავდა. იწერება
//  გაფრთხილება და ჩანაწერი SupportTools-ის განახლებამდე სინქრონიზაციაში აღარ მონაწილეობს
public abstract class RegistrySyncAdapter<TContract> : IRegistrySyncAdapter, IRegistryServerKeys where TContract : class
{
    //ბოლოს წაკითხული სერვერის ყველა ჩანაწერის გასაღები, გამორიცხულების ჩათვლით (IRegistryServerKeys)
    private readonly HashSet<string> _serverKeys = new(StringComparer.OrdinalIgnoreCase);

    //სერვერის ჩანაწერები, რომლებიც ბოლო წაკითხვისას FindUnsupportedField-მა უარყო. ძრავა სერვერის ჩანაწერებს
    //ლოკალურზე ადრე კითხულობს, ამიტომ ლოკალური მხარეც მათ გამორიცხავს
    private readonly HashSet<string> _unsupportedServerKeys = new(StringComparer.OrdinalIgnoreCase);

    protected RegistrySyncAdapter(RegistrySyncWarnings warnings)
    {
        Warnings = warnings;
    }

    protected RegistrySyncWarnings Warnings { get; }

    public IReadOnlyCollection<string> ServerKeys => _serverKeys;

    public abstract string CollectionName { get; }
    public abstract int Order { get; }

    public object Normalize(object contract)
    {
        return NormalizeContract((TContract)contract);
    }

    public IReadOnlyDictionary<string, object> GetLocalRecords()
    {
        Dictionary<string, object> records = [];
        foreach ((string key, TContract contract) in GetLocalContracts())
        {
            if (IsSynced(key) && !_unsupportedServerKeys.Contains(key))
            {
                records.TryAdd(key, contract);
            }
        }

        return records;
    }

    public async Task<Result<IReadOnlyDictionary<string, RegistryServerRecord>>> GetServerRecords(
        CancellationToken cancellationToken)
    {
        Result<List<TContract>> contracts = await GetServerContracts(cancellationToken);
        if (contracts.IsFailure)
        {
            return Result.Failure<IReadOnlyDictionary<string, RegistryServerRecord>>(contracts.Error);
        }

        _unsupportedServerKeys.Clear();
        _serverKeys.Clear();
        Dictionary<string, RegistryServerRecord> records = [];
        foreach (TContract contract in contracts.Value)
        {
            string key = GetKey(contract);
            _serverKeys.Add(key);
            if (!IsSynced(key))
            {
                continue;
            }

            string? unsupportedField = FindUnsupportedField(contract);
            if (unsupportedField is null)
            {
                records.TryAdd(key, new RegistryServerRecord(contract, GetVersion(contract)));
                continue;
            }

            _unsupportedServerKeys.Add(key);
            Warnings.Add(CollectionName, key,
                $"{unsupportedField} of the server record is unknown to this version of SupportTools, " +
                "the record is not synced until SupportTools is updated");
        }

        return Result.Success<IReadOnlyDictionary<string, RegistryServerRecord>>(records);
    }

    public Task<Result<int>> Upsert(string key, object contract, int expectedVersion,
        CancellationToken cancellationToken)
    {
        return UpsertContract(key, (TContract)contract, expectedVersion, cancellationToken);
    }

    public abstract Task<Result> Delete(string key, int expectedVersion, CancellationToken cancellationToken);

    public void ApplyLocal(string key, object contract)
    {
        ApplyContract(key, (TContract)contract);
    }

    public abstract void RemoveLocal(string key);

    protected abstract TContract NormalizeContract(TContract contract);

    //ლოკალური ჩანაწერები კონტრაქტებად: კანონიკური გზებით და კომპიუტერის ველების გარეშე
    protected abstract IEnumerable<(string Key, TContract Contract)> GetLocalContracts();

    protected abstract Task<Result<List<TContract>>> GetServerContracts(CancellationToken cancellationToken);

    protected abstract string GetKey(TContract contract);

    protected abstract int GetVersion(TContract contract);

    protected abstract Task<Result<int>> UpsertContract(string key, TContract contract, int expectedVersion,
        CancellationToken cancellationToken);

    protected abstract void ApplyContract(string key, TContract contract);

    protected virtual bool IsSynced(string key)
    {
        return true;
    }

    //სერვერის კონტრაქტის ველი, რომლის მნიშვნელობაც ამ კლიენტისთვის უცნობია და ლოკალურად ვერ აისახება; null — ასეთი
    //ველი არ არის
    protected virtual string? FindUnsupportedField(TContract contract)
    {
        return null;
    }

    //ძველი git-ის endpoint-ები (Gits და შაბლონები) ვერსიას არ ამოწმებს და ახალ ვერსიას არ აბრუნებს: B1-მა ისინი არ
    //შეცვალა, რადგან ძველი ბრძანებები ვერსიას არ აგზავნის. ამიტომ ამ კოლექციებისთვის B1-ის წესს ადაპტერი თვითონ
    //ამოწმებს (მომხმარებლის გადაწყვეტილება, C3): ჩაწერამდე სერვერის სიაში შენახულ ვერსიას მოსალოდნელს ადარებს, ჩაწერის
    //შემდეგ კი ახალ ვერსიას იმავე სიიდან კითხულობს. შემოწმებასა და ჩაწერას შორის სხვა კომპიუტერის ცვლილება მაინც
    //შეიძლება გადაიწეროს, მაგრამ ეს ფანჯარა მცირეა
    protected async Task<Result<int>> UpsertCheckingVersionOnClient(string key, int expectedVersion,
        Func<Task<Result>> write, CancellationToken cancellationToken)
    {
        Result<int> storedVersion = await GetStoredVersion(key, cancellationToken);
        if (storedVersion.IsFailure)
        {
            return storedVersion;
        }

        Error? versionError = CheckExpectedVersion(key, expectedVersion, storedVersion.Value);
        if (versionError is not null)
        {
            return Result.Failure<int>(versionError);
        }

        Result writeResult = await write();
        if (writeResult.IsFailure)
        {
            return Result.Failure<int>(writeResult.Error);
        }

        Result<int> newVersion = await GetStoredVersion(key, cancellationToken);
        //ჩაწერის შემდეგ ჩანაწერი სიაში უნდა იყოს; თუ არ არის, ის შუალედში წაიშალა
        return newVersion.IsSuccess && newVersion.Value == 0
            ? Result.Failure<int>(SupportToolsServerApiClientErrors.RecordWithNameNotFound(CollectionName, key))
            : newVersion;
    }

    //notFoundErrorCode: ძველი endpoint-ის „ჩანაწერი არ არის“ კოდი (მაგ. GitWithKeyNotFound). ძრავისთვის ის
    //RecordWithNameNotFound-ია: ჩანაწერი სერვერზე უკვე აღარ არის, ანუ წაშლის მიზანი მიღწეულია
    protected async Task<Result> DeleteCheckingVersionOnClient(string key, int expectedVersion,
        Func<Task<Result>> delete, string notFoundErrorCode, CancellationToken cancellationToken)
    {
        Result<int> storedVersion = await GetStoredVersion(key, cancellationToken);
        if (storedVersion.IsFailure)
        {
            return Result.Failure(storedVersion.Error);
        }

        Error? versionError = CheckExpectedVersion(key, expectedVersion, storedVersion.Value);
        if (versionError is not null)
        {
            return Result.Failure(versionError);
        }

        Result deleteResult = await delete();
        return deleteResult.IsFailure && deleteResult.Error.Code == notFoundErrorCode
            ? Result.Failure(SupportToolsServerApiClientErrors.RecordWithNameNotFound(CollectionName, key))
            : deleteResult;
    }

    //სერვერზე შენახული ვერსია; 0 — ჩანაწერი სერვერზე არ არის (B1-ის ვერსიები 1-დან იწყება)
    private async Task<Result<int>> GetStoredVersion(string key, CancellationToken cancellationToken)
    {
        Result<List<TContract>> contracts = await GetServerContracts(cancellationToken);
        if (contracts.IsFailure)
        {
            return Result.Failure<int>(contracts.Error);
        }

        TContract? stored =
            contracts.Value.Find(x => string.Equals(GetKey(x), key, StringComparison.OrdinalIgnoreCase));
        return stored is null ? 0 : GetVersion(stored);
    }

    //B1-ის წესი: მოსალოდნელი ვერსია 0 ნიშნავს, რომ ჩანაწერი სერვერზე არ უნდა იყოს, N კი — რომ სერვერზე ვერსია N-ია
    private Error? CheckExpectedVersion(string key, int expectedVersion, int storedVersion)
    {
        if (storedVersion == expectedVersion)
        {
            return null;
        }

        return storedVersion == 0
            ? SupportToolsServerApiClientErrors.RecordWithNameNotFound(CollectionName, key)
            : SupportToolsServerApiClientErrors.ConcurrencyConflict(CollectionName, key, expectedVersion,
                storedVersion);
    }
}
