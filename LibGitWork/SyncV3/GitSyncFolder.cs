namespace LibGitWork.SyncV3;

//სინქრონიზაციის ერთი ფოლდერი: რომელი git-ის (Gits-ის გასაღები, ის SupportToolsServer-ის სახელიცაა) და რომელი პროექტის.
//ProjectName=null: SupportTools-ის სამუშაო ფოლდერის კლონი (WorkFolder\Gits), რომელსაც V2 ყოველ სინქრონიზაციაზე ახლებს
internal sealed record GitSyncFolder(string GitName, string GitAddress, string? ProjectName, string FolderPath)
{
    public bool IsWorkFolderClone => ProjectName is null;
}
