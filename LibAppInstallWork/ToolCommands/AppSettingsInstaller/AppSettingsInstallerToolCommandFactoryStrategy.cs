using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.ToolCommands.AppSettingsInstaller;

// ReSharper disable once UnusedType.Global
public class AppSettingsInstallerToolCommandFactoryStrategy(
    IApplication app,
    ILogger<AppSettingsInstallerToolCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory) : IToolCommandFactoryStrategy
{
    public string ToolCommandName => nameof(EProjectServerTools.AppSettingsInstaller);

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
            return new AppSettingsInstallerToolCommand(app.AppName, logger, httpClientFactory, true,
                appSettingsInstallerParameters, parametersManager);
        }

        StShared.WriteErrorLine("appSettingsInstallerParameters is null", true);
        return null;
    }
}
