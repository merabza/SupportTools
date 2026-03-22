using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.AppSettingsInstaller;

public class AppSettingsInstallerToolCommandFactoryStrategy : IToolCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppSettingsInstallerToolCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppSettingsInstallerToolCommandFactoryStrategy(
        ILogger<AppSettingsInstallerToolCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string ToolCommandName => nameof(EProjectTools.CorrectNewDatabase);

    public async ValueTask<IToolCommand?> CreateToolCommand(IParametersManager parametersManager,
        IFactoryStrategyParameters factoryStrategyParameters, CancellationToken cancellationToken = default)
    {
        var projectToolsFactoryStrategyParameters =
            (ProjectServerToolsFactoryStrategyParameters)factoryStrategyParameters;

        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;

        //დაშიფრული პარამეტრების განახლება
        var appSettingsInstallerParameters = await AppSettingsInstallerParameters.Create(supportToolsParameters,
            projectToolsFactoryStrategyParameters.ProjectName, projectToolsFactoryStrategyParameters.ServerInfo,
            CancellationToken.None);
        if (appSettingsInstallerParameters is not null)
        {
            return new AppSettingsInstallerToolCommand(_logger, _httpClientFactory, true,
                appSettingsInstallerParameters, parametersManager);
        }

        StShared.WriteErrorLine("appSettingsInstallerParameters is null", true);
        return null;
    }
}
