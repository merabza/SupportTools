namespace LibAppProjectCreator.Git;

public enum GitState
{
    UpToDate,
    NeedToPull,
    NeedToPush,
    Diverged,
    Unknown
}