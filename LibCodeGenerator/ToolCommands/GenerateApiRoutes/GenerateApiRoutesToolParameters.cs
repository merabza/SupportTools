using System;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibCodeGenerator.ToolCommands.GenerateApiRoutes;

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
            var generateApiRoutesToolParameters = new GenerateApiRoutesToolParameters(projectName,
                supportToolsParameters.GetProjectRequired(projectName));
            return generateApiRoutesToolParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}
