using System;
using System.IO;
using LibAppProjectCreator;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibScaffoldSeeder.Models;

public sealed class ExternalScaffoldSeedToolParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private ExternalScaffoldSeedToolParameters(string projectFilePath, string projectParametersFilePath)
    {
        ProjectFilePath = projectFilePath;
        ProjectParametersFilePath = projectParametersFilePath;
    }

    public string ProjectFilePath { get; }
    public string ProjectParametersFilePath { get; set; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ExternalScaffoldSeedToolParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, Func<string, string> projectNameCounter)
    {
        try
        {
            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (string.IsNullOrWhiteSpace(project.DbContextProjectName))
            {
                StShared.WriteErrorLine($"DbContextProjectName does not specified for project {projectName}", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
            {
                StShared.WriteErrorLine("supportToolsParameters.ScaffoldSeedersWorkFolder does not specified", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
            {
                StShared.WriteErrorLine($"ScaffoldSeederProjectName does not specified for project {projectName}",
                    true);
                return null;
            }

            var seedDbProjectName = projectNameCounter(project.DbContextProjectName); //NamingStats.SeedDbProjectName
            var scaffoldSeedersWorkFolder = supportToolsParameters.ScaffoldSeedersWorkFolder;
            var scaffoldSeederProjectName = project.ScaffoldSeederProjectName;
            var scaffoldSeederProjectName2 = projectNameCounter(scaffoldSeederProjectName);

            var seedProjectFilePath = Path.Combine(scaffoldSeedersWorkFolder, scaffoldSeederProjectName,
                scaffoldSeederProjectName2, scaffoldSeederProjectName2, seedDbProjectName,
                $"{seedDbProjectName}{NamingStats.CsProjectExtension}");

            var seedProjectParametersFilePath = Path.Combine(scaffoldSeedersWorkFolder, scaffoldSeederProjectName,
                NamingStats.ScaffoldSeedSecFolderName(scaffoldSeederProjectName), $"{seedDbProjectName}{NamingStats.JsonExtension}");

            var dataSeederParameters =
                new ExternalScaffoldSeedToolParameters(seedProjectFilePath, seedProjectParametersFilePath);
            return dataSeederParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}