using System.IO;
using SystemTools.SharedKernel;

namespace LibGitWork.SyncV3;

//ფოლდერის ლოკალური შემოწმების შედეგი: Ready-ს Status აქვს, Failed-ს ErrorMessage
internal sealed record GitFolderInspection(
    GitSyncFolder Folder,
    EGitFolderInspectionKind Kind,
    GitFolderStatus? Status,
    string? ErrorMessage)
{
    public bool HasChanges => Status?.HasChanges == true;

    //V2-ის Phase1-ის ლოკალური ნაწილი, ქსელის გარეშე: არარსებული ფოლდერი დასაკლონია; არსებული git-ის რეპოზიტორია უნდა
    //იყოს და იმავე მისამართიდან დაკლონილი; .gitignore-ით გამორიცხული ფაილები ქეშიდან იშლება; ცვლილებები (მათ შორის
    //ახალი ფაილები) commit-ისთვის ემზადება (git add .)
    public static GitFolderInspection Inspect(GitFolderCommands commands, GitSyncFolder folder)
    {
        string path = folder.FolderPath;
        if (!Directory.Exists(path))
        {
            return new GitFolderInspection(folder, EGitFolderInspectionKind.NeedsClone, null, null);
        }

        Result<string> originUrlResult = commands.GetOriginUrl(path);
        if (originUrlResult.IsFailure)
        {
            return Failed(folder, $"is not a git repository with an origin: {originUrlResult.Error.Description}");
        }

        if (originUrlResult.Value != folder.GitAddress)
        {
            return Failed(folder, $"is cloned from {originUrlResult.Value}, not from {folder.GitAddress}");
        }

        if (folder.IsWorkFolderClone)
        {
            return InspectWorkFolderClone(commands, folder);
        }

        Result<string[]> redundantFilesResult = commands.GetRedundantCachedFiles(path);
        if (redundantFilesResult.IsFailure)
        {
            return Failed(folder, redundantFilesResult.Error.Description);
        }

        foreach (string fileName in redundantFilesResult.Value)
        {
            Result removeResult = commands.RemoveFromCache(path, fileName);
            if (removeResult.IsFailure)
            {
                return Failed(folder, removeResult.Error.Description);
            }
        }

        Result<GitFolderStatus> statusResult = commands.ReadStatus(path);
        if (statusResult.IsFailure)
        {
            return Failed(folder, statusResult.Error.Description);
        }

        if (statusResult.Value.HasChanges)
        {
            Result addResult = commands.AddAll(path);
            if (addResult.IsFailure)
            {
                return Failed(folder, addResult.Error.Description);
            }
        }

        return new GitFolderInspection(folder, EGitFolderInspectionKind.Ready, statusResult.Value, null);
    }

    //სამუშაო ფოლდერის კლონში არაფერი იცვლება და არაფერი კომიტდება, მხოლოდ remote-ს ეწევა. ცვლილებებს V2 სერვერის
    //ვერსიით ანაცვლებს, ოღონდ კითხვით, ამიტომ აქ ისინი შეცდომაა
    private static GitFolderInspection InspectWorkFolderClone(GitFolderCommands commands, GitSyncFolder folder)
    {
        Result<GitFolderStatus> statusResult = commands.ReadStatus(folder.FolderPath);
        if (statusResult.IsFailure)
        {
            return Failed(folder, statusResult.Error.Description);
        }

        return statusResult.Value.HasChanges
            ? Failed(folder,
                "is the work folder clone of SupportTools and has changes: restore it (V2 offers to) and sync again")
            : new GitFolderInspection(folder, EGitFolderInspectionKind.Ready, statusResult.Value, null);
    }

    private static GitFolderInspection Failed(GitSyncFolder folder, string errorMessage)
    {
        return new GitFolderInspection(folder, EGitFolderInspectionKind.Failed, null, errorMessage);
    }
}
