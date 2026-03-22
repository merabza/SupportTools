using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ServiceStarter;

// ReSharper disable once UnusedType.Global
public class ServiceStarterToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceStarterToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServiceStarterToolCommandFactoryStrategy(ILogger<ServiceStarterToolCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.CorrectNewDatabase);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        var projectName = projectToolsFactoryStrategyParameters.ProjectName;
        var serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //სერვისის გამშვები სერვერის მხარეს
        var serviceStartParameters = ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceStartParameters is not null)
        {
            return new ValueTask<IToolCommand?>(new ServiceStarterToolCommand(_logger, _httpClientFactory,
                serviceStartParameters, parametersManager, true));
        }

        StShared.WriteErrorLine("serviceStartParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
