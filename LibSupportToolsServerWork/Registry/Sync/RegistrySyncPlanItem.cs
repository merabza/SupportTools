namespace LibSupportToolsServerWork.Registry.Sync;

//გეგმის ერთი ჩანაწერი: რა უნდა მოხდეს ერთ ჩანაწერზე და რომელი მონაცემით
public sealed class RegistrySyncPlanItem
{
    public required string CollectionName { get; init; }
    public int Order { get; init; }

    //ლოკალური წერილობა, თუ ჩანაწერი ლოკალურად არსებობს; თორემ სერვერის, თორემ მდგომარეობის
    public required string Key { get; init; }

    public ERegistrySyncAction Action { get; init; }

    //Pull-ისა და Push-ის სახე
    public ERegistrySyncChange Change { get; init; }

    //კონფლიქტის სახე, თუ Action == Conflict
    public ERegistrySyncConflict Conflict { get; init; }

    //ლოკალური ჩანაწერი, თუ არსებობს
    public RegistrySyncRecord? Local { get; init; }

    //სერვერის ჩანაწერი, თუ არსებობს
    public RegistrySyncRecord? Server { get; init; }
}
