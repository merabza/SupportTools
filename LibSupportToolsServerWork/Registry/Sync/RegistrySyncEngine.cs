using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//რეესტრის სინქრონიზაციის ძრავა (README §4.3, G5). CreatePlan ადაპტერებიდან ჩანაწერებს კითხულობს, ნორმალიზებს, ჰეშავს
//და დამგეგმავს (RegistrySyncPlanner) გადასცემს; Execute გეგმიდან არჩეულ ნაწილს ასრულებს (RegistrySyncExecutor).
//მდგომარეობა SupportToolsParameters.RegistrySyncState-შია და მონაცემთან ერთად ერთი Save-ით ინახება
public sealed class RegistrySyncEngine
{
    private const string LocalSide = "local";
    private const string ServerSide = "server";

    private readonly Dictionary<string, IRegistrySyncAdapter> _adapters;
    private readonly IParametersManager _parametersManager;
    private readonly TimeProvider _timeProvider;

    public RegistrySyncEngine(IEnumerable<IRegistrySyncAdapter> adapters, IParametersManager parametersManager) : this(
        adapters, parametersManager, TimeProvider.System)
    {
    }

    //დრო პარამეტრადაა გამოტანილი, რომ ტესტებმა LastSyncUtc შეამოწმონ
    internal RegistrySyncEngine(IEnumerable<IRegistrySyncAdapter> adapters, IParametersManager parametersManager,
        TimeProvider timeProvider)
    {
        //კოლექციის სახელი მდგომარეობის გასაღებია, ამიტომ უნიკალური უნდა იყოს: დუბლიკატზე ToDictionary
        //ArgumentException-ს ისვრის
        _adapters = adapters.ToDictionary(x => x.CollectionName, StringComparer.OrdinalIgnoreCase);
        _parametersManager = parametersManager;
        _timeProvider = timeProvider;
    }

    //გეგმა ყველა ადაპტერზე. თუ რომელიმე ადაპტერმა სერვერის ჩანაწერები ვერ წაიკითხა, მის შეცდომას უცვლელად აბრუნებს
    //(მაგ. ApiRequestFailed, როცა სერვერი მიუწვდომელია). გეგმის აგება არაფერს ცვლის
    public async Task<Result<RegistrySyncPlan>> CreatePlan(CancellationToken cancellationToken = default)
    {
        List<RegistryCollectionSnapshot> collections = [];
        foreach (IRegistrySyncAdapter adapter in _adapters.Values.OrderBy(x => x.Order)
                     .ThenBy(x => x.CollectionName, StringComparer.OrdinalIgnoreCase))
        {
            Result<RegistryCollectionSnapshot> collection = await CreateSnapshot(adapter, cancellationToken);
            if (collection.IsFailure)
            {
                return Result.Failure<RegistrySyncPlan>(collection.Error);
            }

            collections.Add(collection.Value);
        }

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        return RegistrySyncPlanner.CreatePlan(collections, parameters.RegistrySyncState);
    }

    public Task<RegistrySyncReport> Execute(RegistrySyncPlan plan, RegistrySyncSelection selection,
        CancellationToken cancellationToken = default)
    {
        var executor = new RegistrySyncExecutor(_adapters, _parametersManager, _timeProvider);
        return executor.Execute(plan, selection, cancellationToken);
    }

    private static async Task<Result<RegistryCollectionSnapshot>> CreateSnapshot(IRegistrySyncAdapter adapter,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyDictionary<string, RegistryServerRecord>> serverRecordsResult =
            await adapter.GetServerRecords(cancellationToken);
        if (serverRecordsResult.IsFailure)
        {
            return Result.Failure<RegistryCollectionSnapshot>(serverRecordsResult.Error);
        }

        Result<Dictionary<string, RegistrySyncRecord>> localRecords = CreateRecords(adapter, LocalSide,
            adapter.GetLocalRecords().Select(x => (x.Key, x.Value, 0)));
        if (localRecords.IsFailure)
        {
            return Result.Failure<RegistryCollectionSnapshot>(localRecords.Error);
        }

        Result<Dictionary<string, RegistrySyncRecord>> serverRecords = CreateRecords(adapter, ServerSide,
            serverRecordsResult.Value.Select(x => (x.Key, x.Value.Contract, x.Value.Version)));
        if (serverRecords.IsFailure)
        {
            return Result.Failure<RegistryCollectionSnapshot>(serverRecords.Error);
        }

        return new RegistryCollectionSnapshot(adapter.CollectionName, adapter.Order, localRecords.Value,
            serverRecords.Value);
    }

    //კონტრაქტების ნორმალიზაცია და ჰეში. გასაღებები რეგისტრის გარეშე უნიკალური უნდა იყოს (G8). ნორმალიზაცია კი
    //სტაბილური: ნორმალიზებული კონტრაქტის ხელახალი ნორმალიზაცია ჰეშს არ უნდა ცვლიდეს, თორემ ჩანაწერი ყოველ
    //სინქრონიზაციაზე შეცვლილად გამოჩნდებოდა
    private static Result<Dictionary<string, RegistrySyncRecord>> CreateRecords(IRegistrySyncAdapter adapter,
        string side, IEnumerable<(string Key, object Contract, int Version)> contracts)
    {
        List<(string Key, object Contract, int Version)> contractList = [.. contracts];
        List<string> duplicateKeys =
        [
            .. contractList.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1)
                .Select(x => string.Join('/', x.Select(y => y.Key)))
        ];
        if (duplicateKeys.Count > 0)
        {
            return RegistrySyncErrors.DuplicateKeys(adapter.CollectionName, side, string.Join(", ", duplicateKeys));
        }

        Dictionary<string, RegistrySyncRecord> records = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string key, object contract, int version) in contractList)
        {
            object normalized = adapter.Normalize(contract);
            string hash = RegistryContractHasher.ComputeHash(normalized);
            if (RegistryContractHasher.ComputeHash(adapter.Normalize(normalized)) != hash)
            {
                return RegistrySyncErrors.NormalizationIsNotStable(adapter.CollectionName, key);
            }

            records.Add(key, new RegistrySyncRecord(normalized, hash, version));
        }

        return records;
    }
}
