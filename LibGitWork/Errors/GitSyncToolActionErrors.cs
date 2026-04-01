using SystemTools.SystemToolsShared.Errors;

namespace LibGitWork.Errors;

public static class GitSyncToolActionErrors
{
    public static readonly Error CouldNotUpdateGitRemote = new()
    {
        Code = nameof(CouldNotUpdateGitRemote), Name = "Could not Update Git Remote"
    };

    public static readonly Error CouldNotGetGitRemoteId = new()
    {
        Code = nameof(CouldNotGetGitRemoteId), Name = "Could not get git Remote Id"
    };

    public static readonly Error CouldNotGetGitLocalId = new()
    {
        Code = nameof(CouldNotGetGitLocalId), Name = "Could not get git Local Id"
    };

    public static readonly Error CouldNotGetGitBaseId = new()
    {
        Code = nameof(CouldNotGetGitBaseId), Name = "Could not get git Base Id"
    };

    public static readonly Error GetRemoteOriginUrlError = new()
    {
        Code = nameof(GetRemoteOriginUrlError), Name = "Error when detecting Remote Origin Url"
    };

    public static readonly Error GetRedundantCachedFilesListError = new()
    {
        Code = nameof(GetRedundantCachedFilesListError), Name = "Error when getting Redundant Cached Files List"
    };

    public static readonly Error HaveUnTrackedFilesError = new()
    {
        Code = nameof(HaveUnTrackedFilesError), Name = "Error when detecting UnTracked Files"
    };

    public static readonly Error NeedCommitError = new()
    {
        Code = nameof(NeedCommitError), Name = "Error when detecting Need Commit"
    };

    public static Error PropertyIsEmpty(string propertyName)
    {
        return new Error { Code = nameof(PropertyIsEmpty), Name = $"Property {propertyName} Is Empty " };
    }
}
