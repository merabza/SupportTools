namespace LibAppProjectCreator.Models;

public sealed class ProjectFromGit : ProjectBase
{
    public ProjectFromGit(string gitProjectFolderName, string projectName, string projectFullPath,
        string projectFileFullName) : base(projectName, projectFullPath, projectFileFullName, gitProjectFolderName)
    {
        GitProjectFolderName = gitProjectFolderName;
    }

    public string GitProjectFolderName { get; }
}