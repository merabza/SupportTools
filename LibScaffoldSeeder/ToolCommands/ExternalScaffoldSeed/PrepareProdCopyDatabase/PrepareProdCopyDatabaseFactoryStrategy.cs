using System.Threading;
using System.Threading.Tasks;
using LibScaffoldSeeder.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed.PrepareProdCopyDatabase;

public class PrepareProdCopyDatabaseFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly ILogger<PrepareProdCopyDatabaseFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PrepareProdCopyDatabaseFactoryStrategy(ILogger<PrepareProdCopyDatabaseFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string ToolCommandName => nameof(EProjectTools.PrepareProdCopyDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project == null)
        {
            StShared.WriteErrorLine($"Project with name {projectName} not found", true);
            return new ValueTask<IToolCommand?>((IToolCommand?)null);
        }

        var prepareProdCopyDatabaseParameters = ExternalScaffoldSeedToolParameters.Create(supportToolsParameters,
            projectName, null, project.PrepareProdCopyDatabaseProjectFilePath,
            project.PrepareProdCopyDatabaseProjectParametersFilePath);
        if (prepareProdCopyDatabaseParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(
                new ExternalScaffoldSeedToolCommand(_logger, prepareProdCopyDatabaseParameters));
        }

        StShared.WriteErrorLine("dataSeederParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
