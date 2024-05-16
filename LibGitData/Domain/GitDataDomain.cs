namespace LibGitData.Domain;

public sealed class GitDataDomain
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public GitDataDomain(string gitProjectAddress, string gitProjectFolderName, string gitIgnorePathName)
    {
        GitProjectAddress = gitProjectAddress;
        GitProjectFolderName = gitProjectFolderName;
        GitIgnorePathName = gitIgnorePathName;
    }

    public string GitProjectAddress { get; set; }
    public string GitProjectFolderName { get; set; }
    public string GitIgnorePathName { get; }
}