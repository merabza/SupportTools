namespace SupportToolsData;

//პროექტის სოლუშენის აგების (build) შემოწმების სტატუსი
public enum EProjectBuildCheckStatus
{
    //სოლუშენის ფაილის სახელი არ არის მითითებული
    SolutionFileNameIsEmpty,

    //მითითებული სოლუშენის ფაილი არ არსებობს
    SolutionFiledoesNotExists,

    //ფაილი არსებობს, მაგრამ არ არის სოლუშენის ფაილი
    InvalidSolutionFile,

    //სოლუშენის აგება (build) ვერ შესრულდა
    BuildFailed,

    //სოლუშენი წარმატებით აიგო (build)
    Success
}
