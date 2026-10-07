namespace LibSupportToolsServerWork.Registry.Sync;

//გეგმის ჩანაწერის შესრულების შედეგი
public enum ERegistrySyncOutcome
{
    None, //გასაკეთებელი არაფერი იყო: InSync ან Skipped
    NotSelected, //ოპერაცია არჩევანში არ შევიდა, ან კონფლიქტი გამოტოვდა (Skip)
    Done, //ოპერაცია შესრულდა და ჩანაწერის მდგომარეობა განახლდა
    Conflict, //სერვერმა ConcurrencyConflict ან RecordWithNameNotFound დააბრუნა: ჩანაწერი სერვერზე შუალედში შეიცვალა
    Failed, //სერვერმა ან ადაპტერმა ოპერაცია უარყო; მიზეზი Error-შია
    NotExecuted //სერვერის ოპერაცია ან ფაილის შიგთავსის ჩამოტანა ტრანსპორტის შეცდომის გამო აღარ შესრულდა
}
