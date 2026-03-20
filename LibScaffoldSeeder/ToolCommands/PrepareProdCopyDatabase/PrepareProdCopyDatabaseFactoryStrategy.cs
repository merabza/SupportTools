using LibAppProjectCreator;
using LibScaffoldSeeder.Models;
using LibScaffoldSeeder.ToolCommands.JsonFromProjectDbProjectGetter;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using Serilog.Core;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.PrepareProdCopyDatabase;

public class PrepareProdCopyDatabaseFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<PrepareProdCopyDatabaseFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PrepareProdCopyDatabaseFactoryStrategy(ILogger<PrepareProdCopyDatabaseFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.PrepareProdCopyDatabase);

    public IToolCommand CreateToolCommand(IParametersManager parametersManager, string projectName)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return null;
        }

        var prepareProdCopyDatabaseParameters = ExternalScaffoldSeedToolParameters.Create(
            supportToolsParameters, projectName, null, project.PrepareProdCopyDatabaseProjectFilePath,
            project.PrepareProdCopyDatabaseProjectParametersFilePath);
        if (prepareProdCopyDatabaseParameters is not null)
        {
            return new ExternalScaffoldSeedToolCommand(_logger, prepareProdCopyDatabaseParameters);
        }

        StShared.WriteErrorLine("dataSeederParameters is null", true);
        return null;
    }
}
