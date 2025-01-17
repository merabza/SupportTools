using System;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.Models;

public sealed class DataSeederParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public DataSeederParameters(string seedProjectFilePath, string seedProjectParametersFilePath)
    {
        SeedProjectFilePath = seedProjectFilePath;
        SeedProjectParametersFilePath = seedProjectParametersFilePath;
    }

    public string SeedProjectFilePath { get; }
    public string SeedProjectParametersFilePath { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static DataSeederParameters? Create(SupportToolsParameters supportToolsParameters, string projectName)
    {
        try
        {
            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (string.IsNullOrWhiteSpace(project.SeedProjectFilePath))
            {
                StShared.WriteErrorLine($"SeedProjectFilePath does not specified for project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.SeedProjectParametersFilePath))
            {
                StShared.WriteErrorLine($"SeedProjectParametersFilePath does not specified for project {projectName}",
                    true);
                return null;
            }

            var dataSeederParameters = new DataSeederParameters(
                project.SeedProjectFilePath, project.SeedProjectParametersFilePath);
            return dataSeederParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}