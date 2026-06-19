namespace SupportToolsData;

//პროექტის სოლუშენის აგების (build) შემოწმების სტატუსი
public enum EProjectBuildCheckStatus
{
    //სოლუშენის ფაილის სახელი არ არის მითითებული
    SolutionFileNameIsEmpty,

    //მითითებული სოლუშენის ფაილი არ არსებობს
    SolutionFileDoesNotExists,

    //ფაილი არსებობს, მაგრამ არ არის სოლუშენის ფაილი
    InvalidSolutionFile,

    //გაშვებული პროექტის შესაბამისი სოლუშების დაბილდვა არ გამოვა
    CannotBuildSelf,

    //სოლუშენის აგება (build) ვერ შესრულდა
    BuildFailed,

    //სოლუშენი წარმატებით აიგო (build)
    Success
}
