// ReSharper disable ConvertToPrimaryConstructor

using System.IO;

namespace LibAppProjectCreator.Models;

public /*open*/ class ProjectBase
{
    protected ProjectBase(string projectName, string createInPath, string projectFolderName, string projectFileName,
        string? solutionFolderName)
    {
        ProjectName = projectName;
        CreateInPath = createInPath;
        ProjectFolderName = projectFolderName;
        ProjectFileName = projectFileName;
        SolutionFolderName = solutionFolderName;
    }

    public string ProjectName { get; }
    public string CreateInPath { get; }
    public string ProjectFolderName { get; }
    public string ProjectFileName { get; }
    public string? SolutionFolderName { get; }

    public string ProjectFullPath => Path.Combine(CreateInPath, ProjectFolderName);
    public string ProjectFileFullName => Path.Combine(CreateInPath, ProjectFolderName, ProjectFileName);
}