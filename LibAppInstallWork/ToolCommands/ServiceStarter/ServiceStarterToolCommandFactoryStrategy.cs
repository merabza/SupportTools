using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.ServiceStarter;

// ReSharper disable once UnusedType.Global
public class ServiceStarterToolCommandFactoryStrategy(
    ILogger<ServiceStarterToolCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectServerTools.ServiceStarter);

    public ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;
        string projectName = projectToolsFactoryStrategyParameters.ProjectName;
        ServerInfoModel serverInfo = projectToolsFactoryStrategyParameters.ServerInfo;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //სერვისის გამშვები სერვერის მხარეს
        var serviceStartParameters = ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceStartParameters is not null)
        {
            return new ValueTask<IToolCommand?>(new ServiceStarterToolCommand(logger, httpClientFactory,
                serviceStartParameters, parametersManager, true));
        }

        StShared.WriteErrorLine("serviceStartParameters is null", true);
        return new ValueTask<IToolCommand?>((IToolCommand?)null);
    }
}
