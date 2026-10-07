using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Sync;

//ერთი კოლექციის მონაცემი დამგეგმავისთვის: ლოკალური და სერვერის ჩანაწერები, გასაღებით. ფაილების კოლექციისთვის
//(IRegistryFileSyncAdapter) ძრავა ორ დამატებით ველსაც ავსებს
public sealed record RegistryCollectionSnapshot(
    string CollectionName,
    int Order,
    IReadOnlyDictionary<string, RegistrySyncRecord> LocalRecords,
    IReadOnlyDictionary<string, RegistrySyncRecord> ServerRecords)
{
    //წაშლა (Pull(Delete) და Push(Delete)) კონფლიქტად იქცევა, რომ მომხმარებელმა დაადასტუროს
    public bool DeletesNeedConfirmation { get; init; }

    //ჩანაწერები, რომლებიც ამ კომპიუტერზე უნდა იყოს, მაგრამ ლოკალურად აკლია: სერვერიდან ჩამოდის და სერვერიდან არ იშლება
    public IReadOnlyCollection<string> MissingLocalKeys { get; init; } = [];
}
