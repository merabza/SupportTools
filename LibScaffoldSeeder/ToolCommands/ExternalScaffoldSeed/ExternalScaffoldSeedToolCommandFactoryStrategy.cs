using System;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed;

public class ExternalScaffoldSeedToolCommandFactoryStrategy
{
    private readonly ILogger _logger;

    protected ExternalScaffoldSeedToolCommandFactoryStrategy(ILogger logger)
    {
        _logger = logger;
    }

    protected IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName,
        Func<string, string>? externalToolProjectNameCounter, string? externalToolProjectDefFilePath = null,
        string? externalToolProjectDefParametersFilePath = null)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

        if (string.IsNullOrWhiteSpace(supportToolsParameters.ScaffoldSeedersWorkFolder))
        {
            StShared.WriteErrorLine("supportToolsParameters.ScaffoldSeedersWorkFolder does not specified", true);
            return null;
        }

        if (externalToolProjectNameCounter is null)
        {
            StShared.WriteErrorLine("externalToolProjectNameCounter is null", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
        {
            StShared.WriteErrorLine($"ScaffoldSeederProjectName does not specified for project {projectName}", true);
            return null;
        }

        string externalToolProjectName = externalToolProjectNameCounter(project.ScaffoldSeederProjectName);

        //არსებული პროდაქშენ ბაზის ასლიდან დაამზადებს json ფაილები თავიდან
        var jsonFromProjectDbProjectGetterParameters = ExternalScaffoldSeedToolParameters.Create(supportToolsParameters,
            projectName, externalToolProjectNameCounter, externalToolProjectDefFilePath,
            externalToolProjectDefParametersFilePath);
        if (jsonFromProjectDbProjectGetterParameters is not null)
        {
            return new ExternalScaffoldSeedToolCommand(_logger, jsonFromProjectDbProjectGetterParameters);
        }

        StShared.WriteErrorLine($"{externalToolProjectName} parameters is null", true);
        return null;
    }
}
