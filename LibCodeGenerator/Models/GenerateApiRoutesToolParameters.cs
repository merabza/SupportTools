using System;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibCodeGenerator.Models;

public sealed class GenerateApiRoutesToolParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private GenerateApiRoutesToolParameters(string projectName, ProjectModel project)
    {
        ProjectName = projectName;
        Project = project;
    }

    public string ProjectName { get; set; }
    public ProjectModel Project { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static GenerateApiRoutesToolParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName)
    {
        try
        {
            var externalScaffoldSeedToolParameters = new GenerateApiRoutesToolParameters(projectName,
                supportToolsParameters.GetProjectRequired(projectName));
            return externalScaffoldSeedToolParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}