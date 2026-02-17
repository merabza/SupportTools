namespace LibGitWork;

public enum GitState
{
    UpToDate,
    NeedToPull,
    NeedToPush,
    Diverged,
    Unknown
}
