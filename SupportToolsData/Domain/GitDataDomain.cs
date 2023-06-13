namespace SupportToolsData.Domain;

public sealed class GitDataDomain
{
    public GitDataDomain(string gitProjectAddress, string gitProjectFolderName)
    {
        GitProjectAddress = gitProjectAddress;
        GitProjectFolderName = gitProjectFolderName;
    }

    public string GitProjectAddress { get; set; }
    public string GitProjectFolderName { get; set; }
}