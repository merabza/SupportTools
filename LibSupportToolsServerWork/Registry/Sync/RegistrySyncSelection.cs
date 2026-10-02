using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Sync;

//რა შესრულდეს გეგმიდან. C5 მომხმარებლის არჩევანს აწყობს, D2 — ავტომატურ Pull-ს და Push-ს
public sealed class RegistrySyncSelection
{
    //ყველა არაკონფლიქტური ოპერაცია
    public static RegistrySyncSelection AllNonConflicting { get; } = new() { IncludePulls = true, IncludePushes = true };

    //მხოლოდ არაკონფლიქტური Pull-ები
    public static RegistrySyncSelection PullOnly { get; } = new() { IncludePulls = true };

    //მხოლოდ არაკონფლიქტური Push-ები
    public static RegistrySyncSelection PushOnly { get; } = new() { IncludePushes = true };

    public bool IncludePulls { get; init; }
    public bool IncludePushes { get; init; }

    //კონფლიქტების გადაწყვეტა გეგმის ჩანაწერების მიხედვით. კონფლიქტი, რომელიც აქ არ არის, გამოიტოვება (Skip)
    public IReadOnlyDictionary<RegistrySyncPlanItem, ERegistryConflictResolution> ConflictResolutions { get; init; } =
        new Dictionary<RegistrySyncPlanItem, ERegistryConflictResolution>();
}
