using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//გეგმის ერთი შესრულება (README §4.3). რიგი:
//1. სერვერის ოპერაციები: upsert-ები Order-ის ზრდადობით, მერე წაშლები კლებადობით;
//2. ლოკალური ცვლილებები ადგილზე: ApplyLocal ზრდადობით, მერე RemoveLocal კლებადობით;
//3. ერთი IParametersManager.Save, თუ ლოკალური მონაცემი ან მდგომარეობა შეიცვალა.
//ყოველი წარმატებული ოპერაციის შემდეგ ჩანაწერის მდგომარეობა ახლდება: სერვერის ახალი ვერსია და ლოკალური ჰეში.
//სერვერის შეცდომების ცნობა RegistrySyncServerErrorCodes-შია აღწერილი
internal sealed class RegistrySyncExecutor
{
    private readonly IReadOnlyDictionary<string, IRegistrySyncAdapter> _adapters;
    private readonly SupportToolsParameters _parameters;
    private readonly IParametersManager _parametersManager;
    private readonly Dictionary<RegistrySyncPlanItem, RegistrySyncReportItem> _reportItems = [];
    private readonly TimeProvider _timeProvider;
    private bool _changed;
    private Error? _transportError;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RegistrySyncExecutor(IReadOnlyDictionary<string, IRegistrySyncAdapter> adapters,
        IParametersManager parametersManager, TimeProvider timeProvider)
    {
        _adapters = adapters;
        _parametersManager = parametersManager;
        _parameters = (SupportToolsParameters)parametersManager.Parameters;
        _timeProvider = timeProvider;
    }

    private RegistrySyncStateModel State => _parameters.RegistrySyncState;

    public async Task<RegistrySyncReport> Execute(RegistrySyncPlan plan, RegistrySyncSelection selection,
        CancellationToken cancellationToken)
    {
        List<(RegistrySyncPlanItem Item, EOperation Operation)> operations = [];
        foreach (RegistrySyncPlanItem item in plan.Items)
        {
            EOperation operation = GetOperation(item, selection);
            if (operation == EOperation.None)
            {
                RecordWithoutOperation(item);
            }
            else
            {
                operations.Add((item, operation));
            }
        }

        foreach (RegistrySyncPlanItem item in Ascending(operations, EOperation.Upsert))
        {
            await Upsert(item, cancellationToken);
        }

        foreach (RegistrySyncPlanItem item in Descending(operations, EOperation.Delete))
        {
            await Delete(item, cancellationToken);
        }

        RunLocalOperations(Ascending(operations, EOperation.ApplyLocal),
            Descending(operations, EOperation.RemoveLocal));

        bool saved = await SaveIfChanged(cancellationToken);
        return new RegistrySyncReport
        {
            Items = [.. plan.Items.Select(x => _reportItems[x])],
            TransportError = _transportError,
            Changed = _changed,
            Saved = saved
        };
    }

    private static EOperation GetOperation(RegistrySyncPlanItem item, RegistrySyncSelection selection)
    {
        return item.Action switch
        {
            ERegistrySyncAction.Pull when selection.IncludePulls => ToLocal(item),
            ERegistrySyncAction.Push when selection.IncludePushes => ToServer(item),
            ERegistrySyncAction.Conflict => ResolveConflict(item,
                selection.ConflictResolutions.GetValueOrDefault(item)),
            _ => EOperation.None
        };
    }

    private static EOperation ResolveConflict(RegistrySyncPlanItem item, ERegistryConflictResolution resolution)
    {
        return resolution switch
        {
            ERegistryConflictResolution.Local => ToServer(item),
            ERegistryConflictResolution.Server => ToLocal(item),
            _ => EOperation.None
        };
    }

    //სერვერი ლოკალურს უთანაბრდება: ლოკალურად წაშლილი სერვერიდანაც იშლება
    private static EOperation ToServer(RegistrySyncPlanItem item)
    {
        return item.Local is null ? EOperation.Delete : EOperation.Upsert;
    }

    //ლოკალური სერვერისას უთანაბრდება: სერვერზე წაშლილი ლოკალურადაც იშლება
    private static EOperation ToLocal(RegistrySyncPlanItem item)
    {
        return item.Server is null ? EOperation.RemoveLocal : EOperation.ApplyLocal;
    }

    //Order-ის მიხედვით დალაგება სტაბილურია, ამიტომ ერთი Order-ის ფარგლებში გეგმის რიგი (კოლექცია, გასაღები) რჩება
    private static List<RegistrySyncPlanItem> Ascending(List<(RegistrySyncPlanItem Item, EOperation Operation)> operations,
        EOperation operation)
    {
        return [.. operations.Where(x => x.Operation == operation).Select(x => x.Item).OrderBy(x => x.Order)];
    }

    private static List<RegistrySyncPlanItem> Descending(
        List<(RegistrySyncPlanItem Item, EOperation Operation)> operations, EOperation operation)
    {
        return
        [
            .. operations.Where(x => x.Operation == operation).Select(x => x.Item).OrderByDescending(x => x.Order)
        ];
    }

    private void RecordWithoutOperation(RegistrySyncPlanItem item)
    {
        if (item.Action == ERegistrySyncAction.InSync)
        {
            UpdateInSyncState(item);
        }

        Record(item,
            item.Action is ERegistrySyncAction.InSync or ERegistrySyncAction.Skipped
                ? ERegistrySyncOutcome.None
                : ERegistrySyncOutcome.NotSelected);
    }

    //InSync ჩანაწერის მდგომარეობა სერვერის ვერსიას და ლოკალურ ჰეშს უნდა ასახავდეს. პირველი სინქრონიზაციისას და
    //თანაბარი შიგთავსისას ის ახლა იწერება. ორივე მხარეს წაშლილი ჩანაწერი მდგომარეობიდანაც იშლება
    private void UpdateInSyncState(RegistrySyncPlanItem item)
    {
        if (item.Local is not null && item.Server is not null)
        {
            SetRecordState(item, item.Server.Version, item.Local.Hash);
        }
        else
        {
            RemoveRecordState(item);
        }
    }

    private async Task Upsert(RegistrySyncPlanItem item, CancellationToken cancellationToken)
    {
        if (StopIfServerIsUnavailable(item))
        {
            return;
        }

        //Upsert-ს ლოკალური ჩანაწერი ყოველთვის აქვს (ToServer). სერვერზე არარსებული ჩანაწერი 0 ვერსიით იქმნება
        RegistrySyncRecord local = item.Local!;
        Result<int> result = await _adapters[item.CollectionName]
            .Upsert(item.Key, local.Contract, item.Server?.Version ?? 0, cancellationToken);
        if (result.IsFailure)
        {
            RecordServerError(item, result.Error);
            return;
        }

        SetRecordState(item, result.Value, local.Hash);
        Record(item, ERegistrySyncOutcome.Done);
    }

    private async Task Delete(RegistrySyncPlanItem item, CancellationToken cancellationToken)
    {
        if (StopIfServerIsUnavailable(item))
        {
            return;
        }

        //Delete-ს სერვერის ჩანაწერი ყოველთვის აქვს: ლოკალურად წაშლილი ჩანაწერი დამგეგმავმა სერვერზე იპოვა
        Result result = await _adapters[item.CollectionName]
            .Delete(item.Key, item.Server!.Version, cancellationToken);
        //RecordWithNameNotFound: ჩანაწერი სერვერზე უკვე აღარ არის, ანუ წაშლის მიზანი მიღწეულია
        if (result.IsFailure && result.Error.Code != RegistrySyncServerErrorCodes.RecordWithNameNotFound)
        {
            RecordServerError(item, result.Error);
            return;
        }

        RemoveRecordState(item);
        Record(item, ERegistrySyncOutcome.Done);
    }

    //ტრანსპორტის შეცდომის შემდეგ სერვერის დარჩენილი ოპერაციები აღარ სრულდება
    private bool StopIfServerIsUnavailable(RegistrySyncPlanItem item)
    {
        if (_transportError is null)
        {
            return false;
        }

        Record(item, ERegistrySyncOutcome.NotExecuted);
        return true;
    }

    private void RecordServerError(RegistrySyncPlanItem item, Error error)
    {
        if (error.Code == RegistrySyncServerErrorCodes.RequestFailed)
        {
            _transportError = error;
        }

        ERegistrySyncOutcome outcome = error.Code switch
        {
            RegistrySyncServerErrorCodes.ConcurrencyConflict or RegistrySyncServerErrorCodes.RecordWithNameNotFound =>
                ERegistrySyncOutcome.Conflict,
            _ => ERegistrySyncOutcome.Failed
        };
        Record(item, outcome, error);
    }

    private void RunLocalOperations(List<RegistrySyncPlanItem> applyItems, List<RegistrySyncPlanItem> removeItems)
    {
        foreach (RegistrySyncPlanItem item in applyItems)
        {
            //ApplyLocal-ს სერვერის ჩანაწერი ყოველთვის აქვს (ToLocal)
            _adapters[item.CollectionName].ApplyLocal(item.Key, item.Server!.Contract);
            _changed = true;
        }

        foreach (RegistrySyncPlanItem item in removeItems)
        {
            _adapters[item.CollectionName].RemoveLocal(item.Key);
            _changed = true;
        }

        //ლოკალური ჩანაწერები ხელახლა იკითხება, თითო კოლექციაზე ერთხელ. მდგომარეობაში ის ჰეში ჩაიწერება, რომელსაც
        //ადაპტერი შემდეგ სინქრონიზაციაზე მისცემს, და მოწმდება, რომ ცვლილება ნამდვილად მოხდა. თორემ ჩამოტანილი, მაგრამ
        //ლოკალურად არგამოჩენილი ჩანაწერი შემდეგ სინქრონიზაციაზე ლოკალურად წაშლილად ჩაითვლებოდა და სერვერიდანაც წაიშლებოდა
        foreach (IGrouping<string, RegistrySyncPlanItem> collectionItems in applyItems.Concat(removeItems)
                     .GroupBy(x => x.CollectionName, StringComparer.OrdinalIgnoreCase))
        {
            IRegistrySyncAdapter adapter = _adapters[collectionItems.Key];
            ILookup<string, object> localRecords = adapter.GetLocalRecords()
                .ToLookup(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            foreach (RegistrySyncPlanItem item in collectionItems)
            {
                RecordLocalResult(adapter, item, [.. localRecords[item.Key]]);
            }
        }
    }

    private void RecordLocalResult(IRegistrySyncAdapter adapter, RegistrySyncPlanItem item, List<object> localContracts)
    {
        //RemoveLocal
        if (item.Server is null)
        {
            if (localContracts.Count > 0)
            {
                Record(item, ERegistrySyncOutcome.Failed,
                    RegistrySyncErrors.LocalRecordNotRemoved(item.CollectionName, item.Key));
                return;
            }

            RemoveRecordState(item);
            Record(item, ERegistrySyncOutcome.Done);
            return;
        }

        //ApplyLocal
        if (localContracts.Count != 1)
        {
            Record(item, ERegistrySyncOutcome.Failed,
                RegistrySyncErrors.LocalRecordNotApplied(item.CollectionName, item.Key));
            return;
        }

        SetRecordState(item, item.Server.Version,
            RegistryContractHasher.ComputeHash(adapter.Normalize(localContracts[0])));
        Record(item, ERegistrySyncOutcome.Done);
    }

    //მდგომარეობა ადგილზე ახლდება: მისი dictionary-ები არ იცვლება
    private void SetRecordState(RegistrySyncPlanItem item, int version, string hash)
    {
        if (State.Collections.TryGetValue(item.CollectionName, out RegistryCollectionSyncStateModel? collection) &&
            collection.Records.TryGetValue(item.Key, out RegistryRecordSyncStateModel? current) &&
            current.Version == version && current.Hash == hash)
        {
            return;
        }

        State.GetOrAddCollection(item.CollectionName).Records[item.Key] =
            new RegistryRecordSyncStateModel { Version = version, Hash = hash };
        _changed = true;
    }

    private void RemoveRecordState(RegistrySyncPlanItem item)
    {
        if (State.Collections.TryGetValue(item.CollectionName, out RegistryCollectionSyncStateModel? collection) &&
            collection.Records.Remove(item.Key))
        {
            _changed = true;
        }
    }

    private void Record(RegistrySyncPlanItem item, ERegistrySyncOutcome outcome, Error? error = null)
    {
        _reportItems[item] = new RegistrySyncReportItem(item, outcome, error);
    }

    private async Task<bool> SaveIfChanged(CancellationToken cancellationToken)
    {
        if (!_changed)
        {
            return false;
        }

        State.LastSyncUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return await _parametersManager.Save(_parameters, string.Empty, null, cancellationToken);
    }

    private enum EOperation
    {
        None,
        Upsert,
        Delete,
        ApplyLocal,
        RemoveLocal
    }
}
