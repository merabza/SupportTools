using SystemTools.SystemToolsShared.Errors;

namespace LibGitWork.Errors;

public static class GitSyncToolActionErrors
{
    public static readonly ErrorOmd CouldNotUpdateGitRemote = new()
    {
        Code = nameof(CouldNotUpdateGitRemote), Name = "Could not Update Git Remote"
    };

    public static readonly ErrorOmd CouldNotGetGitRemoteId = new()
    {
        Code = nameof(CouldNotGetGitRemoteId), Name = "Could not get git Remote Id"
    };

    public static readonly ErrorOmd CouldNotGetGitLocalId = new()
    {
        Code = nameof(CouldNotGetGitLocalId), Name = "Could not get git Local Id"
    };

    public static readonly ErrorOmd CouldNotGetGitBaseId = new()
    {
        Code = nameof(CouldNotGetGitBaseId), Name = "Could not get git Base Id"
    };

    public static readonly ErrorOmd GetRemoteOriginUrlError = new()
    {
        Code = nameof(GetRemoteOriginUrlError), Name = "ErrorOmd when detecting Remote Origin Url"
    };

    public static readonly ErrorOmd GetRedundantCachedFilesListError = new()
    {
        Code = nameof(GetRedundantCachedFilesListError), Name = "ErrorOmd when getting Redundant Cached Files List"
    };

    public static readonly ErrorOmd HaveUnTrackedFilesError = new()
    {
        Code = nameof(HaveUnTrackedFilesError), Name = "ErrorOmd when detecting UnTracked Files"
    };

    public static readonly ErrorOmd NeedCommitError = new()
    {
        Code = nameof(NeedCommitError), Name = "ErrorOmd when detecting Need Commit"
    };

    public static ErrorOmd PropertyIsEmpty(string propertyName)
    {
        return new ErrorOmd { Code = nameof(PropertyIsEmpty), Name = $"Property {propertyName} Is Empty " };
    }
}
