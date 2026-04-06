using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed.SeedData;

// ReSharper disable once UnusedType.Global
public class SeedDataFactoryStrategy : ExternalScaffoldSeedToolCommandFactoryStrategy, IToolCommandFactoryStrategy
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SeedDataFactoryStrategy(ILogger<SeedDataFactoryStrategy> logger) : base(logger)
    {
    }

    public string ToolCommandName => nameof(EProjectTools.SeedData);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        ProjectModel? project = supportToolsParameters.GetProject(projectName);
        if (project != null)
        {
            return ValueTask.FromResult<IToolCommand?>(CreateToolCommand(parametersManager, projectName,
                NamingStats.SeedDbProjectName, project.SeedProjectFilePath, project.SeedProjectParametersFilePath));
        }

        StShared.WriteErrorLine($"Project with name {projectName} not found", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
