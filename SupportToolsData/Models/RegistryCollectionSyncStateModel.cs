using System;
using System.Collections.Generic;

namespace SupportToolsData.Models;

//რეესტრის ერთი კოლექციის სინქრონიზაციის მდგომარეობა
public sealed class RegistryCollectionSyncStateModel
{
    //ჩანაწერის გასაღები → სერვერის Version და ლოკალური კონტრაქტის ჰეში ბოლო წარმატებული სინქრონიზაციისას
    public Dictionary<string, RegistryRecordSyncStateModel> Records { get; } = new(StringComparer.OrdinalIgnoreCase);

    //სინქრონიზაციიდან გამორიცხული ჩანაწერების გასაღებები, მაგალითად Linux-ისთვის განსხვავებული DB კავშირი. ასეთი
    //ჩანაწერი არც სერვერზე მიდის და არც სერვერიდან ჩამოდის
    public HashSet<string> ExcludedKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
}
