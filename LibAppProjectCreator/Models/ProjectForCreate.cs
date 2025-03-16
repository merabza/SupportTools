using System.Collections.Generic;
using System.IO;
using LibDotnetWork;

namespace LibAppProjectCreator.Models;

public sealed class ProjectForCreate : ProjectBase
{
    private ProjectForCreate(string projectName, EDotnetProjectType dotnetProjectType, string projectCreateParameters,
        string createInPath, string projectFolderName, string projectFileName, string? classForDelete,
        string? solutionFolderName) : base(projectName, createInPath, projectFolderName, projectFileName,
        solutionFolderName)
    {
        DotnetProjectType = dotnetProjectType;
        ProjectCreateParameters = projectCreateParameters;
        ClassForDelete = classForDelete;
    }

    public EDotnetProjectType DotnetProjectType { get; }
    public string ProjectCreateParameters { get; }
    public string? ClassForDelete { get; }
    public Dictionary<string, string> FoldersForCreate { get; } = [];


    public static ProjectForCreate CreateReactProject(string createInPath, string projectName,
        string[] availableFolders, string? solutionFolderName = null)
    {
        return Create(createInPath, projectName, projectName, EDotnetProjectType.ReactEsProj, string.Empty, null,
            availableFolders, solutionFolderName);
    }

    public static ProjectForCreate CreateClassLibProject(string createInPath, string projectName,
        string[] availableFolders, string? solutionFolderName = null)
    {
        return Create(createInPath, projectName, projectName, EDotnetProjectType.ClassLib, string.Empty, "Class1",
            availableFolders, solutionFolderName);
    }

    public static ProjectForCreate Create(string createInPath, string projectFolderName, string projectName,
        EDotnetProjectType dotnetProjectType, string projectCreateParameters, string? classForDelete,
        string[] availableFolders, string? solutionFolderName = null)
    {
        var projectFileName = $"{projectName}.{(dotnetProjectType == EDotnetProjectType.ReactEsProj ? "e" : "c")}sproj";
        var projectDataModel = new ProjectForCreate(projectName, dotnetProjectType, projectCreateParameters,
            createInPath, projectFolderName, projectFileName, classForDelete, solutionFolderName);

        foreach (var folder in availableFolders)
        {
            var folderFullPath = Path.Combine(createInPath, projectFolderName, folder);
            //ჩაემატოს პროექტის ფოლდერებში
            projectDataModel.AddFolder(folder, folderFullPath);
        }

        return projectDataModel;
    }

    private void AddFolder(string folder, string folderFullPath)
    {
        FoldersForCreate.Add(folder, folderFullPath);
    }
}