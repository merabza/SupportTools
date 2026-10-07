using System;
using System.Collections.Generic;
using System.Linq;
using SupportToolsData.Models;

namespace LibSupportToolsServerWork.Registry.Sync;

//სინქრონიზაციის დამგეგმავი (README §4.3). სუფთა ფუნქციაა: ლოკალური ჩანაწერებიდან, სერვერის ჩანაწერებიდან და ბოლო
//სინქრონიზაციის მდგომარეობიდან თითო ჩანაწერის მოქმედებას ადგენს, არაფერს ცვლის და არაფერს იძახებს.
//- ExcludedKeys-ის ჩანაწერი → Skipped.
//- ჩანაწერი ორივე მხარესაა:
//  - შიგთავსი (ჰეში) თანაბარია → InSync, მდგომარეობის განახლებით. ასე ხდება მაშინაც, როცა რომელიმე მხარე შეიცვალა,
//    რადგან გადასატანი მაინც არაფერია;
//  - მდგომარეობაში არ არის (პირველი სინქრონიზაცია, ან ჩანაწერი ორივე მხარეს დამოუკიდებლად დაემატა) →
//    Conflict(FirstSyncDiffers);
//  - localChanged = (ჰეში ≠ state.Hash), serverChanged = (Version ≠ state.Version): არცერთი → InSync, მხოლოდ სერვერი →
//    Pull(Update), მხოლოდ ლოკალური → Push(Update), ორივე → Conflict(BothChanged).
//- ჩანაწერი მხოლოდ ლოკალურადაა: მდგომარეობაში არ არის → Push(Add). არის, ანუ სერვერზე წაიშალა: ლოკალური არ შეცვლილა →
//  Pull(Delete), შეიცვალა → Conflict(DeletedOnServer).
//- ჩანაწერი მხოლოდ სერვერზეა: მდგომარეობაში არ არის → Pull(Add). არის, ანუ ლოკალურად წაიშალა: სერვერზე არ შეცვლილა →
//  Push(Delete), შეიცვალა → Conflict(DeletedLocally).
//- ჩანაწერი მხოლოდ მდგომარეობაშია, ანუ ორივე მხარეს წაიშალა → InSync; შემსრულებელი მას მდგომარეობიდან შლის.
//ფაილების კოლექციაში (IRegistryFileSyncAdapter, C6) ორი გამონაკლისია:
//- ჩანაწერი, რომელიც ლოკალურად აკლია, მაგრამ აქ უნდა იყოს (MissingLocalKeys), ლოკალურად წაშლილად არ ითვლება: თუ
//  სერვერზეა → Pull(Add), მდგომარეობის მიუხედავად;
//- Pull(Delete) და Push(Delete) → Conflict(DeleteNeedsConfirmation): წაშლას მომხმარებელი ადასტურებს.
//გასაღებები რეგისტრის გარეშე შედარდება (G8)
public static class RegistrySyncPlanner
{
    public static RegistrySyncPlan CreatePlan(IEnumerable<RegistryCollectionSnapshot> collections,
        RegistrySyncStateModel state)
    {
        List<RegistrySyncPlanItem> items = [];
        foreach (RegistryCollectionSnapshot collection in collections.OrderBy(x => x.Order)
                     .ThenBy(x => x.CollectionName, StringComparer.OrdinalIgnoreCase))
        {
            state.Collections.TryGetValue(collection.CollectionName,
                out RegistryCollectionSyncStateModel? collectionState);
            items.AddRange(PlanCollection(collection, collectionState));
        }

        return new RegistrySyncPlan(items);
    }

    private static List<RegistrySyncPlanItem> PlanCollection(RegistryCollectionSnapshot collection,
        RegistryCollectionSyncStateModel? collectionState)
    {
        //შემავალი dictionary-ები შეიძლება რეგისტრზე მგრძნობიარე იყოს, ამიტომ ძებნა რეგისტრის გარეშე ასლებში ხდება.
        //გასაღებები რეგისტრის გარეშეც უნიკალური უნდა იყოს: ამას ძრავა ამოწმებს (RegistrySyncErrors.DuplicateKeys)
        Dictionary<string, RegistrySyncRecord> localRecords = new(collection.LocalRecords,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, RegistrySyncRecord> serverRecords = new(collection.ServerRecords,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, RegistryRecordSyncStateModel> stateRecords = new(collectionState?.Records ?? [],
            StringComparer.OrdinalIgnoreCase);
        HashSet<string> excludedKeys = new(collectionState?.ExcludedKeys ?? [], StringComparer.OrdinalIgnoreCase);
        HashSet<string> missingLocalKeys = new(collection.MissingLocalKeys, StringComparer.OrdinalIgnoreCase);

        //ერთი ჩანაწერის გასაღები სამივე წყაროში შეიძლება სხვადასხვა რეგისტრით ეწეროს. გეგმაში რჩება პირველი: ლოკალური,
        //მერე სერვერის, მერე მდგომარეობის
        HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);
        return
        [
            .. localRecords.Keys.Concat(serverRecords.Keys).Concat(stateRecords.Keys).Where(seenKeys.Add)
                .Order(StringComparer.OrdinalIgnoreCase).Select(key => PlanRecord(collection, key,
                    localRecords.GetValueOrDefault(key), serverRecords.GetValueOrDefault(key),
                    stateRecords.GetValueOrDefault(key), excludedKeys.Contains(key), missingLocalKeys.Contains(key)))
        ];
    }

    private static RegistrySyncPlanItem PlanRecord(RegistryCollectionSnapshot collection, string key,
        RegistrySyncRecord? local, RegistrySyncRecord? server, RegistryRecordSyncStateModel? state, bool isExcluded,
        bool isMissingLocally)
    {
        Decision decision = isExcluded ? Decision.Skipped : Decide(local, server, state, isMissingLocally);
        //ფაილების კოლექციაში წაშლას მომხმარებელი წყვეტს
        if (collection.DeletesNeedConfirmation && decision.Change == ERegistrySyncChange.Delete)
        {
            decision = Decision.Conflicted(ERegistrySyncConflict.DeleteNeedsConfirmation);
        }

        return new RegistrySyncPlanItem
        {
            CollectionName = collection.CollectionName,
            Order = collection.Order,
            Key = key,
            Action = decision.Action,
            Change = decision.Change,
            Conflict = decision.Conflict,
            Local = local,
            Server = server
        };
    }

    private static Decision Decide(RegistrySyncRecord? local, RegistrySyncRecord? server,
        RegistryRecordSyncStateModel? state, bool isMissingLocally)
    {
        if (local is not null && server is not null)
        {
            return DecideOnBothSides(local, server, state);
        }

        if (local is not null)
        {
            return DecideOnLocalOnly(local, state);
        }

        //ჩანაწერი მხოლოდ მდგომარეობაშია: ორივე მხარეს წაიშალა
        if (server is null)
        {
            return Decision.InSync;
        }

        //ლოკალურად აკლია, მაგრამ აქ უნდა იყოს: ლოკალურად წაშლილი არ არის და სერვერიდან ჩამოდის
        return isMissingLocally ? Decision.Pull(ERegistrySyncChange.Add) : DecideOnServerOnly(server, state);
    }

    private static Decision DecideOnBothSides(RegistrySyncRecord local, RegistrySyncRecord server,
        RegistryRecordSyncStateModel? state)
    {
        if (local.Hash == server.Hash)
        {
            return Decision.InSync;
        }

        if (state is null)
        {
            return Decision.Conflicted(ERegistrySyncConflict.FirstSyncDiffers);
        }

        bool localChanged = local.Hash != state.Hash;
        bool serverChanged = server.Version != state.Version;
        return (localChanged, serverChanged) switch
        {
            (true, true) => Decision.Conflicted(ERegistrySyncConflict.BothChanged),
            (false, true) => Decision.Pull(ERegistrySyncChange.Update),
            (true, false) => Decision.Push(ERegistrySyncChange.Update),
            _ => Decision.InSync
        };
    }

    private static Decision DecideOnLocalOnly(RegistrySyncRecord local, RegistryRecordSyncStateModel? state)
    {
        if (state is null)
        {
            return Decision.Push(ERegistrySyncChange.Add);
        }

        //სერვერზე წაიშალა
        return local.Hash == state.Hash
            ? Decision.Pull(ERegistrySyncChange.Delete)
            : Decision.Conflicted(ERegistrySyncConflict.DeletedOnServer);
    }

    private static Decision DecideOnServerOnly(RegistrySyncRecord server, RegistryRecordSyncStateModel? state)
    {
        if (state is null)
        {
            return Decision.Pull(ERegistrySyncChange.Add);
        }

        //ლოკალურად წაიშალა
        return server.Version == state.Version
            ? Decision.Push(ERegistrySyncChange.Delete)
            : Decision.Conflicted(ERegistrySyncConflict.DeletedLocally);
    }

    private readonly record struct Decision(
        ERegistrySyncAction Action,
        ERegistrySyncChange Change,
        ERegistrySyncConflict Conflict)
    {
        public static Decision InSync =>
            new(ERegistrySyncAction.InSync, ERegistrySyncChange.None, ERegistrySyncConflict.None);

        public static Decision Skipped =>
            new(ERegistrySyncAction.Skipped, ERegistrySyncChange.None, ERegistrySyncConflict.None);

        public static Decision Pull(ERegistrySyncChange change)
        {
            return new Decision(ERegistrySyncAction.Pull, change, ERegistrySyncConflict.None);
        }

        public static Decision Push(ERegistrySyncChange change)
        {
            return new Decision(ERegistrySyncAction.Push, change, ERegistrySyncConflict.None);
        }

        public static Decision Conflicted(ERegistrySyncConflict conflict)
        {
            return new Decision(ERegistrySyncAction.Conflict, ERegistrySyncChange.None, conflict);
        }
    }
}
