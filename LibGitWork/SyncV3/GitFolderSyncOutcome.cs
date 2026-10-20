namespace LibGitWork.SyncV3;

//ერთი ფოლდერის სინქრონიზაციის შედეგი. ლოკალური შემოწმების შეცდომა აქ Error-ად გადადის
internal sealed class GitFolderSyncOutcome
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public GitFolderSyncOutcome(GitFolderInspection inspection)
    {
        Inspection = inspection;
        Error = inspection.ErrorMessage;
    }

    public GitFolderInspection Inspection { get; }
    public GitSyncFolder Folder => Inspection.Folder;
    public bool Cloned { get; set; }
    public bool Committed { get; set; }
    public bool Fetched { get; set; }
    public bool Pulled { get; set; }
    public bool Pushed { get; set; }

    //upstream-ის გარეშე branch, რომელიც ამ სინქრონიზაციამ origin-ზე გამოაქვეყნა (Pushed-იც true-ა)
    public string? PublishedBranch { get; set; }

    public string? Error { get; set; }

    //git სერვერთან მიმართვის გარეშე: არც დაკლონილა, არც fetch გაკეთებულა და შეცდომაც არ ყოფილა
    public bool WithoutRequest => !Cloned && !Fetched && Error is null;
}
