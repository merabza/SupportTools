using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Sync;

//რა შესრულდეს გეგმიდან. C5 მომხმარებლის არჩევანს აწყობს, D2 — ავტომატურ Pull-ს და Push-ს
public sealed class RegistrySyncSelection
{
    //ყველა არაკონფლიქტური ოპერაცია
    public static RegistrySyncSelection AllNonConflicting { get; } =
        new() { IncludePulls = true, IncludePushes = true };

    //მხოლოდ არაკონფლიქტური Pull-ები
    public static RegistrySyncSelection PullOnly { get; } = new() { IncludePulls = true };

    //მხოლოდ არაკონფლიქტური Push-ები
    public static RegistrySyncSelection PushOnly { get; } = new() { IncludePushes = true };

    public bool IncludePulls { get; init; }

    //IncludePulls-ისას Pull(Delete)-ებიც: სერვერზე წაშლილი ჩანაწერი ლოკალურადაც იშლება. D2-ის გაშვებისას ავტომატური
    //Pull მათ არ ასრულებს (მომხმარებლის გადაწყვეტილება): ცარიელმა ან სხვა სერვერმა ლოკალური რეესტრი ჩუმად არ უნდა
    //წაშალოს. კონფლიქტის გადაწყვეტას (ConflictResolutions) ეს არ ეხება
    public bool IncludePullDeletes { get; init; } = true;

    public bool IncludePushes { get; init; }

    //სერვერის პირველი წარუმატებელი ოპერაციის (Failed) შემდეგ სერვერის დარჩენილი ოპერაციები აღარ სრულდება და
    //NotExecuted ხდება, ისევე როგორც ტრანსპორტის შეცდომისას; ლოკალური ოპერაციები მაინც სრულდება. C5 ასე ასობით
    //მოთხოვნიდან (seed) პირველივე შეცდომაზე ჩერდება, რადგან ჩავარდნილ ჩანაწერზე დამოკიდებული ჩანაწერებიც ჩავარდებოდა.
    //Conflict შედეგი გაჩერებას არ იწვევს
    public bool StopOnFailure { get; init; }

    //კონფლიქტების გადაწყვეტა გეგმის ჩანაწერების მიხედვით. კონფლიქტი, რომელიც აქ არ არის, გამოიტოვება (Skip)
    public IReadOnlyDictionary<RegistrySyncPlanItem, ERegistryConflictResolution> ConflictResolutions { get; init; } =
        new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution>();
}
