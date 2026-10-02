namespace SupportToolsData.Models;

//ჩანაწერის მდგომარეობა ბოლო წარმატებული სინქრონიზაციისას: სერვერის Version და ლოკალური კონტრაქტის ჰეში
public sealed class RegistryRecordSyncStateModel
{
    public int Version { get; init; }
    public string? Hash { get; init; }
}
