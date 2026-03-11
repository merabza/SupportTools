using System.IO;
using AppCliTools.CliMenu;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public /*open*/ class CloneInfoFileCliMenuCommand : CliMenuCommand
{
    protected CloneInfoFileCliMenuCommand(string menuName) : base(menuName, EMenuAction.LevelUp)
    {
    }

    protected static string? GetDefCloneFileName(SupportToolsParameters parameters, ProjectModel project)
    {
        string? defCloneFile = null;
        string? projectFolderName = null;
        if (!string.IsNullOrWhiteSpace(project.ProjectFolderName) &&
            !string.IsNullOrWhiteSpace(project.MainProjectName))
        {
            string? gitName = parameters.GitProjects[project.MainProjectName].GitName;
            if (string.IsNullOrWhiteSpace(gitName))
            {
                return null;
            }

            string? gitProjectFolderName = parameters.Gits[gitName].GitProjectFolderName;
            if (string.IsNullOrWhiteSpace(gitProjectFolderName))
            {
                return null;
            }

            projectFolderName = Path.Combine(project.ProjectFolderName, gitProjectFolderName);
        }

        if (projectFolderName == null)
        {
            return null;
        }

        var projectFolder = new DirectoryInfo(projectFolderName);
        if (projectFolder.Exists)
        {
            defCloneFile = Path.Combine(projectFolder.FullName, "CloneInfo.txt");
        }

        return defCloneFile;
    }
}
