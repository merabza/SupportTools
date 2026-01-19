using ParametersManagement.LibParameters;

namespace LibGitData.Models;

public sealed class GitDataModelRem : ItemData
{
    public string? GitProjectAddress { get; set; }
    public string? GitProjectFolderName { get; set; }
    public string? GitIgnorePathName { get; set; }
    public string? GitProjectRemoteName { get; set; }
    public required string GitProjectName { get; set; }
}