namespace LibGitWork.Models;

public sealed class GitData
{
    public required string GitProjectName { get; set; }
    public required string GitProjectAddress { get; set; }
    public required string GitProjectFolderName { get; set; }
    public required string GitIgnorePathName { get; set; }
}
