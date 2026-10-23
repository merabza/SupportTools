using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibGitWork;

public static class GitStat
{
    public static bool CheckGitProject(string projectName, ProjectModel project)
    {
        if (!string.IsNullOrWhiteSpace(project.ProjectFolderName))
        {
            return true;
        }

        StShared.WriteErrorLine($"ProjectFolderName is not specified for project {projectName}", true);
        return false;
    }
}
