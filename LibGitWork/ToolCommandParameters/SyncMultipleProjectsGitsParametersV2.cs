using System.Collections.Generic;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace LibGitWork.ToolCommandParameters;

public sealed class SyncMultipleProjectsGitsParametersV2 : IParameters
{
    private SyncMultipleProjectsGitsParametersV2(Dictionary<string, ProjectModel> projects, string? projectGroupName,
        string? projectName, bool includeRemoteProjects)
    {
        Projects = projects;
        ProjectGroupName = projectGroupName;
        ProjectName = projectName;
        IncludeRemoteProjects = includeRemoteProjects;
    }

    public Dictionary<string, ProjectModel> Projects { get; }
    public string? ProjectGroupName { get; }
    public string? ProjectName { get; }

    //false: მხოლოდ ამ კომპიუტერზე არსებული პროექტები სინქრონიზდება, დანარჩენები არ იკლონება (D3)
    public bool IncludeRemoteProjects { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static SyncMultipleProjectsGitsParametersV2 Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName, string? projectName, bool includeRemoteProjects)
    {
        return new SyncMultipleProjectsGitsParametersV2(supportToolsParameters.Projects, projectGroupName,
            projectName, includeRemoteProjects);
    }
}
