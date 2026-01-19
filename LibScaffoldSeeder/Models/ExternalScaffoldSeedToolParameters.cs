using System;
using System.IO;
using LibAppProjectCreator;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

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
        string projectName, Func<string, string>? externalToolProjectNameCounter,
        string? externalToolProjectDefFilePath = null, string? externalToolProjectDefParametersFilePath = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(externalToolProjectDefFilePath) &&
                File.Exists(externalToolProjectDefFilePath) &&
                !string.IsNullOrWhiteSpace(externalToolProjectDefParametersFilePath) &&
                File.Exists(externalToolProjectDefParametersFilePath))
                return new ExternalScaffoldSeedToolParameters(externalToolProjectDefFilePath,
                    externalToolProjectDefParametersFilePath);

            var project = supportToolsParameters.GetProjectRequired(projectName);

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

            if (externalToolProjectNameCounter is null)
            {
                StShared.WriteErrorLine("externalToolProjectNameCounter is null", true);
                return null;
            }

            var externalToolProjectName = externalToolProjectNameCounter(project.ScaffoldSeederProjectName);
            var scaffoldSeedersWorkFolder = supportToolsParameters.ScaffoldSeedersWorkFolder;
            var scaffoldSeederProjectName = project.ScaffoldSeederProjectName;
            var scaffoldSeederFolderName = NamingStats.ScaffoldSeederFolderName(project.ScaffoldSeederProjectName);

            var externalToolProjectFilePath = Path.Combine(scaffoldSeedersWorkFolder, scaffoldSeederProjectName,
                scaffoldSeederFolderName, scaffoldSeederFolderName, externalToolProjectName,
                $"{externalToolProjectName}{NamingStats.CsProjectExtension}");

            var externalToolProjectParametersFilePath = Path.Combine(scaffoldSeedersWorkFolder,
                scaffoldSeederProjectName, NamingStats.ScaffoldSeedSecFolderName(scaffoldSeederProjectName),
                $"{externalToolProjectName}{NamingStats.JsonExtension}");

            var externalScaffoldSeedToolParameters = new ExternalScaffoldSeedToolParameters(externalToolProjectFilePath,
                externalToolProjectParametersFilePath);
            return externalScaffoldSeedToolParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}