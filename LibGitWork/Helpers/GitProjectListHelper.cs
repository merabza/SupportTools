using System.Collections.Generic;
using System.Linq;
using SupportToolsData.Models;

namespace LibGitWork.Helpers;

public static class GitProjectListHelper
{
    public static IEnumerable<KeyValuePair<string, ProjectModel>> CreateProjectsList(
        Dictionary<string, ProjectModel> projects, string? projectGroupName, string? projectName)
    {
        if (projectGroupName is null && projectName is null)
        {
            return projects;
        }

        if (projectGroupName is not null)
        {
            return projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == projectGroupName);
        }

        return projects.Where(x => x.Key == projectName);
    }
}
