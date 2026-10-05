using SupportToolsServerApiContracts.Errors;
using SystemTools.ApiContracts.Errors;

namespace LibSupportToolsServerWork.Registry.Sync;

//სერვერის შეცდომების კოდები (Error.Code), რომლებსაც შემსრულებელი ადაპტერის Upsert-ისა და Delete-ის შედეგში ცნობს.
//ApiClient კოდს ProblemDetails-ის title-იდან კითხულობს. ConcurrencyConflict და RecordWithNameNotFound B1-ის ზოგადი
//ფაბრიკების სახელებია (SupportToolsServerApiClientErrors); ადაპტერებიც, რომლებიც ვერსიას თვითონ ამოწმებს, ამ
//ფაბრიკებით ქმნის შეცდომას.
//- ConcurrencyConflict (409): ჩანაწერი სერვერზე შუალედში შეიცვალა ან უკვე არსებობს → შედეგი Conflict, დანარჩენი
//  გრძელდება.
//- RecordWithNameNotFound (404): Upsert-ზე ჩანაწერი სერვერზე შუალედში წაიშალა → Conflict, დანარჩენი გრძელდება.
//  Delete-ზე ჩანაწერი სერვერზე უკვე აღარ არის, ანუ წაშლის მიზანი მიღწეულია → Done და მდგომარეობიდან წაშლა.
//- ApiRequestFailed (ApiClientErrors): ქსელის შეცდომა ან timeout, ანუ სერვერი მიუწვდომელია → ეს ოპერაცია Failed,
//  სერვერის დარჩენილი ოპერაციები NotExecuted; უკვე შესრულებულები მდგომარეობაში რჩება, ლოკალური ოპერაციები სრულდება.
//- ყველა სხვა კოდი (მაგ. RecordIsInUse, ReferencedRecordsNotFound, ვალიდაციის შეცდომები, სერვერის 5xx): სერვერმა ეს
//  ჩანაწერი უარყო → Failed, დანარჩენი გრძელდება
public static class RegistrySyncServerErrorCodes
{
    public const string ConcurrencyConflict = nameof(SupportToolsServerApiClientErrors.ConcurrencyConflict);
    public const string RecordWithNameNotFound = nameof(SupportToolsServerApiClientErrors.RecordWithNameNotFound);
    public const string RequestFailed = nameof(ApiClientErrors.ApiRequestFailed);
}
