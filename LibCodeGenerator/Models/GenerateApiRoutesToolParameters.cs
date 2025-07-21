using System;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibCodeGenerator.Models;

public sealed class GenerateApiRoutesToolParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private GenerateApiRoutesToolParameters(ProjectModel project)
    {
        Project = project;
    }

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
            var externalScaffoldSeedToolParameters =
                new GenerateApiRoutesToolParameters(supportToolsParameters.GetProjectRequired(projectName));
            return externalScaffoldSeedToolParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}