namespace LibAppProjectCreator.Models;

public sealed class ProjectFromGit : ProjectBase
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectFromGit(string gitProjectFolderName, string projectName, string createInPath, string projectFolderName,
        string projectFileName) : base(projectName, createInPath, projectFolderName, projectFileName, gitProjectFolderName)
    {
    }

    public string GitProjectFolderName => SolutionFolderName!;
}