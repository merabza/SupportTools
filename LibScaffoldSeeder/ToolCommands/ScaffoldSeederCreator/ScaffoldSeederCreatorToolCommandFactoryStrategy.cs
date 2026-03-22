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
public class ScaffoldSeederCreatorToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ScaffoldSeederCreatorToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ScaffoldSeederCreatorToolCommandFactoryStrategy(
        ILogger<ScaffoldSeederCreatorToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.ScaffoldSeederCreator);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters = (ProjectToolsFactoryStrategyParameters)factoryStrategyParameters;

        //სკაფოლდინგისა და სიდინგის პროექტების შექმნა
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        var scaffoldSeederCreatorParameters = ScaffoldSeederCreatorParameters.Create(_logger, supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, true);
        if (scaffoldSeederCreatorParameters is not null)
        {
            return ValueTask.FromResult<IToolCommand?>(new ScaffoldSeederCreatorToolCommand(_logger, _httpClientFactory,
                true, scaffoldSeederCreatorParameters, parametersManager));
        }

        StShared.WriteErrorLine("scaffoldSeederCreatorParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
