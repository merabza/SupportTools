namespace LibAppProjectCreator.Models;

public sealed class GitCloneDataModel
{
    public GitCloneDataModel(string workPath, string gitProjectName, string gitProjectFolderName)
    {
        WorkPath = workPath;
        GitProjectName = gitProjectName;
        GitProjectFolderName = gitProjectFolderName;
    }

    public string WorkPath { get; set; }
    public string GitProjectName { get; set; }
    public string GitProjectFolderName { get; set; }
}