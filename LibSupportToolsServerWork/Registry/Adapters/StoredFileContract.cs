using Newtonsoft.Json;

namespace LibSupportToolsServerWork.Registry.Adapters;

//საიდუმლო ფაილის კონტრაქტი სინქრონიზაციის ძრავისთვის (C6): ფაილის მეტამონაცემები შიგთავსის გარეშე, როგორც სერვერის
//სია (StsStoredFileInfoDataModel) აბრუნებს. ჩანაწერები Sha256-ითა და Length-ით შედარდება, შიგთავსი კი მხოლოდ
//გაგზავნისა და ჩამოტანისას იკითხება და არსად იბეჭდება. Sha256 შიგთავსის UTF-8 ბაიტების (BOM-ის გარეშე) SHA-256-ია,
//hex დიდი ასოებით, როგორც სერვერი ითვლის (B8); Length იმავე ბაიტების რაოდენობაა. Path ჩანაწერის გასაღებია (კანონიკური
//გზა) და ჰეშში არ შედის: მხოლოდ რეგისტრით განსხვავებული წერილობა ერთი და იგივე ფაილია
public sealed class StoredFileContract
{
    [JsonIgnore] public required string Path { get; init; }

    public required string Sha256 { get; init; }
    public int Length { get; init; }

    //სერვერის ჩანაწერის ვერსია; ლოკალური ჩანაწერისა 0-ია. ზედა დონის Version ჰეშში არ შედის (RegistryContractHasher)
    public int Version { get; init; }
}
