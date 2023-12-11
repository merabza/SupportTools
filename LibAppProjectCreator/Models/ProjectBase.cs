// ReSharper disable ConvertToPrimaryConstructor
namespace LibAppProjectCreator.Models;

public /*open*/ class ProjectBase
{
    public ProjectBase(string projectName, string projectFullPath, string projectFileFullName,
        string? solutionFolderName)
    {
        ProjectName = projectName;
        ProjectFullPath = projectFullPath;
        ProjectFileFullName = projectFileFullName;
        SolutionFolderName = solutionFolderName;
    }

    public string ProjectFullPath { get; }
    public string ProjectName { get; }
    public string ProjectFileFullName { get; }
    public string? SolutionFolderName { get; }
}