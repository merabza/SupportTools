using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Sync;

//ერთი კოლექციის მონაცემი დამგეგმავისთვის: ლოკალური და სერვერის ჩანაწერები, გასაღებით
public sealed record RegistryCollectionSnapshot(
    string CollectionName,
    int Order,
    IReadOnlyDictionary<string, RegistrySyncRecord> LocalRecords,
    IReadOnlyDictionary<string, RegistrySyncRecord> ServerRecords);
