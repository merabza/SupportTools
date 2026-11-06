using System.Collections.Generic;
using LibGitData.Models;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace LibGitWork.ToolCommandParameters;

public sealed class UpdateOutdatedPackagesParameters : IParameters
{
    private UpdateOutdatedPackagesParameters(Dictionary<string, ProjectModel> projects,
        Dictionary<string, GitDataModel> gits, string? projectGroupName, string? projectName)
    {
        Projects = projects;
        Gits = gits;
        ProjectGroupName = projectGroupName;
        ProjectName = projectName;
    }

    public Dictionary<string, ProjectModel> Projects { get; }

    //git-ის ტიპი (GitIgnorePatternName) განახლების რიგისთვის: არა-C# git-ები რიგში არ ითვლება
    public Dictionary<string, GitDataModel> Gits { get; }
    public string? ProjectGroupName { get; }
    public string? ProjectName { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static UpdateOutdatedPackagesParameters Create(SupportToolsParameters supportToolsParameters,
        string? projectGroupName, string? projectName)
    {
        return new UpdateOutdatedPackagesParameters(supportToolsParameters.Projects, supportToolsParameters.Gits,
            projectGroupName, projectName);
    }
}
