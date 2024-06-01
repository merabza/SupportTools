using System;
using LibGitData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork;

public static class GitStat
{
    public static bool CheckGitProject(string projectName, ProjectModel project, EGitCol gitCol,
        bool writeWarning = true)
    {
        switch (gitCol)
        {
            case EGitCol.Main:
                if (!string.IsNullOrWhiteSpace(project.ProjectFolderName))
                    break;
                StShared.WriteErrorLine($"ProjectFolderName is not specified for project {projectName}", true);
                return false;
            case EGitCol.ScaffoldSeed:
                if (!string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
                    break;
                if (writeWarning)
                    StShared.WriteWarningLine($"ScaffoldSeederProjectName is not specified for project {projectName}",
                        true);
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(gitCol), gitCol, null);
        }

        return true;
    }
}