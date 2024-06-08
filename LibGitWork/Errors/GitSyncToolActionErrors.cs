using SystemToolsShared.Errors;

namespace LibGitWork.Errors;

public static class GitSyncToolActionErrors
{
    public static readonly Err GetRemoteOriginUrlError = new()
    {
        ErrorCode = nameof(GetRemoteOriginUrlError),
        ErrorMessage = "Error when detecting Remote Origin Url"
    };

    public static readonly Err GetRedundantCachedFilesListError = new()
    {
        ErrorCode = nameof(GetRedundantCachedFilesListError),
        ErrorMessage = "Error when getting Redundant Cached Files List"
    };

    public static readonly Err HaveUnTrackedFilesError = new()
    {
        ErrorCode = nameof(HaveUnTrackedFilesError),
        ErrorMessage = "Error when detecting UnTracked Files"
    };

    public static readonly Err NeedCommitError = new()
    {
        ErrorCode = nameof(NeedCommitError),
        ErrorMessage = "Error when detecting Need Commit"
    };


    public static Err PropertyIsEmpty(string propertyName)
    {
        return new Err { ErrorCode = nameof(PropertyIsEmpty), ErrorMessage = $"Property {propertyName} Is Empty " };
    }
}