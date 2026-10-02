using SystemTools.SystemToolsShared;

namespace SupportToolsData.Models;

//გზის გარდაქმნის წესი: კანონიკური (Windows-ის ფორმის, როგორც მთავარ კომპიუტერზე) prefix-ი ამ კომპიუტერის
//ლოკალურ prefix-ად იცვლება. მაგალითად D:\1WorkDotnet → /home/merab/1WorkDotnet
public sealed class PathMappingModel : ItemData
{
    public string? CanonicalPrefix { get; set; }
    public string? LocalPrefix { get; set; }

    public override string? GetItemKey()
    {
        return CanonicalPrefix;
    }
}
