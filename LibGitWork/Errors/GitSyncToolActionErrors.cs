using SystemTools.SharedKernel;

namespace LibGitWork.Errors;

public static class GitSyncToolActionErrors
{
    public static readonly Error CouldNotUpdateGitRemote =
        Error.Problem(nameof(CouldNotUpdateGitRemote), "Could not Update Git Remote");

    public static readonly Error CouldNotGetGitRemoteId =
        Error.Problem(nameof(CouldNotGetGitRemoteId), "Could not get git Remote Id");

    public static readonly Error CouldNotGetGitLocalId =
        Error.Problem(nameof(CouldNotGetGitLocalId), "Could not get git Local Id");

    public static readonly Error CouldNotGetGitBaseId =
        Error.Problem(nameof(CouldNotGetGitBaseId), "Could not get git Base Id");

    public static readonly Error GetRemoteOriginUrlError =
        Error.Problem(nameof(GetRemoteOriginUrlError), "Error when detecting Remote Origin Url");

    public static readonly Error GetRedundantCachedFilesListError =
        Error.Problem(nameof(GetRedundantCachedFilesListError), "Error when getting Redundant Cached Files List");

    public static readonly Error HaveUnTrackedFilesError =
        Error.Problem(nameof(HaveUnTrackedFilesError), "Error when detecting UnTracked Files");

    public static readonly Error NeedCommitError =
        Error.Problem(nameof(NeedCommitError), "Error when detecting Need Commit");

    public static Error PropertyIsEmpty(string propertyName)
    {
        return Error.Problem(nameof(PropertyIsEmpty), $"Property {propertyName} Is Empty ");
    }
}
