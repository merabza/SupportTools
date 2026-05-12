using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibScaffoldSeeder.ToolCommands.ScaffoldSeederCreator;

// ReSharper disable once UnusedType.Global
public class ScaffoldSeederCreatorToolCommandFactoryStrategy(
    ILogger<ScaffoldSeederCreatorToolCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectTools.ScaffoldSeederCreator);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var scaffoldSeederCreatorParameters = ScaffoldSeederCreatorParameters.Create(logger, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, true);
        if (scaffoldSeederCreatorParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(new ScaffoldSeederCreatorToolCommand(logger, httpClientFactory,
                true, scaffoldSeederCreatorParameters, parametersManager));
        }

        StShared.WriteErrorLine("scaffoldSeederCreatorParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
