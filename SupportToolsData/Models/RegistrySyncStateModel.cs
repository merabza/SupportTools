using System;
using System.Collections.Generic;

namespace SupportToolsData.Models;

//რეესტრის სინქრონიზაციის ლოკალური მდგომარეობა (README §4.3): ბოლო წარმატებული სინქრონიზაციის შედეგი თითო კოლექციისა
//და ჩანაწერისთვის. SupportToolsParameters.RegistrySyncState-შია, ამიტომ მონაცემთან ერთად, ერთი Save-ით ინახება.
//ეს კომპიუტერის ველია და სერვერზე არ მიდის.
//გასაღებები რეგისტრის გარეშე შედარდება (G8). კოლექციების თვისებებს setter არ აქვს: Newtonsoft ჩატვირთვისას
//ინიციალიზატორით შექმნილ OrdinalIgnoreCase ეგზემპლარს ავსებს, ახალს არ ქმნის და ვერც სხვა კოდი ჩაანაცვლებს მას
public sealed class RegistrySyncStateModel
{
    //ბოლო სინქრონიზაციის დრო, რომელმაც ლოკალური მონაცემი ან მდგომარეობა შეცვალა
    public DateTime? LastSyncUtc { get; set; }

    //კოლექციის სახელი (ადაპტერის CollectionName) → კოლექციის მდგომარეობა
    public Dictionary<string, RegistryCollectionSyncStateModel> Collections { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public RegistryCollectionSyncStateModel GetOrAddCollection(string collectionName)
    {
        if (!Collections.TryGetValue(collectionName, out RegistryCollectionSyncStateModel? collection))
        {
            collection = new RegistryCollectionSyncStateModel();
            Collections.Add(collectionName, collection);
        }

        return collection;
    }
}
