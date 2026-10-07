using System.Collections.Generic;

namespace LibSupportToolsServerWork.Registry.Sync;

//გეგმიდან არჩევანით (RegistrySyncSelection) არჩეული ოპერაციები, შესრულების რიგით:
//- ServerItems: სერვერის ოპერაციები. upsert-ები Order-ის ზრდადობით, მერე წაშლები კლებადობით. ჩანაწერი, რომლის
//  ლოკალური მხარე (Local) არ არის, სერვერიდან იშლება, დანარჩენი სერვერზე იწერება;
//- LocalItems: ლოკალური ცვლილებები. ApplyLocal ზრდადობით, მერე RemoveLocal კლებადობით. ჩანაწერი, რომლის სერვერის
//  მხარე (Server) არ არის, ლოკალურად იშლება, დანარჩენი სერვერიდან ჩამოდის.
//შემსრულებელი სწორედ ამ სიებს ასრულებს, ამიტომ C5 მათით Dry run-ს და არჩევანის სტატუსებს აჩვენებს
public sealed record RegistrySyncOperations(
    IReadOnlyList<RegistrySyncPlanItem> ServerItems,
    IReadOnlyList<RegistrySyncPlanItem> LocalItems);
