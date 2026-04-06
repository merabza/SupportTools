using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.AppSettingsUpdater;

public class AppSettingsUpdaterToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppSettingsUpdaterToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppSettingsUpdaterToolCommandFactoryStrategy(ILogger<AppSettingsUpdaterToolCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectServerTools.AppSettingsUpdater);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //პარამეტრების დაშიფვრა და დაინსტალირებული პროგრამისთვის ამ დაშიფრული პარამეტრების გადაგზავნა-განახლება
        //+(EncodeParameters=>UploadParameters=>DownloadParameters=>UpdateParameters)
        var appSettingsUpdaterParameters = await AppSettingsUpdaterParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, projectToolsFactoryStrategyParameters.ServerInfo,
            cancellationToken);
        if (appSettingsUpdaterParameters is not null)
        {
            return new AppSettingsUpdaterToolCommand(_logger, _httpClientFactory, appSettingsUpdaterParameters,
                parametersManager, true);
        }

        StShared.WriteErrorLine("appSettingsUpdaterParameters is null", true);
        return null;
    }
}
