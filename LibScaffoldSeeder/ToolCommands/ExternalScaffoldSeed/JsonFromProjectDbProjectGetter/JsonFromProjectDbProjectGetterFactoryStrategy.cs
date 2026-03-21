using System.Threading;
using System.Threading.Tasks;
using LibAppProjectCreator;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;

namespace LibScaffoldSeeder.ToolCommands.ExternalScaffoldSeed.JsonFromProjectDbProjectGetter;

public class JsonFromProjectDbProjectGetterFactoryStrategy : ExternalScaffoldSeedToolCommandFactoryStrategy,
    IToolCommandFactoryStrategy
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public JsonFromProjectDbProjectGetterFactoryStrategy(ILogger<JsonFromProjectDbProjectGetterFactoryStrategy> logger)
        : base(logger)
    {
    }

    public string ToolCommandName => nameof(EProjectTools.JsonFromProjectDbProjectGetter);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        return ValueTask.FromResult<IToolCommand?>(CreateToolCommand(parametersManager,
            projectToolsFactoryStrategyParameters.ProjectName, NamingStats.GetJsonFromScaffoldDbProjectName));
    }
}
