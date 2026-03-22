using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.ToolCommands.ServiceStarter;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ServiceStopper;

// ReSharper disable once UnusedType.Global
public class ServiceStopperToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceStopperToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServiceStopperToolCommandFactoryStrategy(ILogger<ServiceStopperToolCommandFactoryStrategy> logger,
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
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        var serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //სერვისის გამაჩერებელი სერვერის მხარეს
        var serviceStopParameters = ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceStopParameters is not null)
        {
            return new ValueTask<IToolCommand?>(new ServiceStopperToolCommand(_logger, _httpClientFactory,
                serviceStopParameters, parametersManager, true));
        }

        StShared.WriteErrorLine("serviceStopParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
