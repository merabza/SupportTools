using System.Collections.Generic;
using SystemTools.SharedKernel;

namespace LibSupportToolsServerWork.Registry.Sync;

//გეგმის შესრულების ანგარიში, რომელსაც C5 აჩვენებს: შედეგი გეგმის ყოველ ჩანაწერზე, გეგმის რიგით
public sealed class RegistrySyncReport
{
    public required IReadOnlyList<RegistrySyncReportItem> Items { get; init; }

    //ტრანსპორტის შეცდომა (სერვერი მიუწვდომელია), რომლის შემდეგაც სერვერის დარჩენილი ოპერაციები აღარ შესრულდა
    public Error? TransportError { get; init; }

    //ლოკალური მონაცემი ან მდგომარეობა შეიცვალა, ამიტომ Save გამოიძახა
    public bool Changed { get; init; }

    //Save-მა წარმატებით შეინახა
    public bool Saved { get; init; }
}
