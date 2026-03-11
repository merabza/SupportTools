using SystemTools.SystemToolsShared.Errors;

namespace LibGitWork.Errors;

public static class GitSyncToolActionErrors
{
    public static readonly Err CouldNotUpdateGitRemote = new()
    {
        ErrorCode = nameof(CouldNotUpdateGitRemote), ErrorMessage = "Could not Update Git Remote"
    };

    public static readonly Err CouldNotGetGitRemoteId = new()
    {
        ErrorCode = nameof(CouldNotGetGitRemoteId), ErrorMessage = "Could not get git Remote Id"
    };

    public static readonly Err CouldNotGetGitLocalId = new()
    {
        ErrorCode = nameof(CouldNotGetGitLocalId), ErrorMessage = "Could not get git Local Id"
    };

    public static readonly Err CouldNotGetGitBaseId = new()
    {
        ErrorCode = nameof(CouldNotGetGitBaseId), ErrorMessage = "Could not get git Base Id"
    };

    public static readonly Err GetRemoteOriginUrlError = new()
    {
        ErrorCode = nameof(GetRemoteOriginUrlError), ErrorMessage = "Error when detecting Remote Origin Url"
    };

    public static readonly Err GetRedundantCachedFilesListError = new()
    {
        ErrorCode = nameof(GetRedundantCachedFilesListError),
        ErrorMessage = "Error when getting Redundant Cached Files List"
    };

    public static readonly Err HaveUnTrackedFilesError = new()
    {
        ErrorCode = nameof(HaveUnTrackedFilesError), ErrorMessage = "Error when detecting UnTracked Files"
    };

    public static readonly Err NeedCommitError = new()
    {
        ErrorCode = nameof(NeedCommitError), ErrorMessage = "Error when detecting Need Commit"
    };

    public static Err PropertyIsEmpty(string propertyName)
    {
        return new Err { ErrorCode = nameof(PropertyIsEmpty), ErrorMessage = $"Property {propertyName} Is Empty " };
    }
}
