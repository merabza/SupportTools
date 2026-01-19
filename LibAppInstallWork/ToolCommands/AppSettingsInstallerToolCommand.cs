using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using ToolsManagement.Installer.ToolActions;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class AppSettingsInstallerToolCommand : ToolCommand
{
    private const string ActionName = "Install Application Settings";

    private const string ActionDescription =
        "This tool will Download latest parameters file from exchange server, then will update parameters file, and check if parameters updated";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _logger;
    private readonly bool _useConsole;

    public AppSettingsInstallerToolCommand(ILogger logger, IHttpClientFactory httpClientFactory, bool useConsole,
        AppSettingsInstallerParameters parameters, IParametersManager parametersManager) : base(logger, ActionName,
        parameters, parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _useConsole = useConsole;
    }

    private AppSettingsInstallerParameters AppSettingsInstallerParameters => (AppSettingsInstallerParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(AppSettingsInstallerParameters.ServerInfo.ServerName))
        {
            _logger.LogError("Server name is not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(AppSettingsInstallerParameters.ServerInfo.EnvironmentName))
        {
            _logger.LogError("Environment name is not specified");
            return false;
        }

        var projectName = AppSettingsInstallerParameters.ProjectName;
        //1. მოვქაჩოთ ფაილსაცავში არსებული უახლესი პარამეტრების ფაილის შიგთავსი.
        var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(_logger, _useConsole,
            AppSettingsInstallerParameters.FileStorageForDownload, AppSettingsInstallerParameters.ProjectName,
            AppSettingsInstallerParameters.ServerInfo.ServerName,
            AppSettingsInstallerParameters.ServerInfo.EnvironmentName,
            AppSettingsInstallerParameters.ParametersFileDateMask,
            AppSettingsInstallerParameters.ParametersFileExtension, null, null);
        var result = await getLatestParametersFileBodyAction.Run(cancellationToken);
        var appSettingsVersion = getLatestParametersFileBodyAction.AppSettingsVersion;
        if (!result || string.IsNullOrWhiteSpace(appSettingsVersion))
        {
            _logger.LogError("Parameters version does not detected");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        var installParametersAction = new InstallParametersAction(_logger, _httpClientFactory,
            AppSettingsInstallerParameters.ParametersFileDateMask,
            AppSettingsInstallerParameters.ParametersFileExtension,
            AppSettingsInstallerParameters.InstallerBaseParameters, AppSettingsInstallerParameters.FileStorageForUpload,
            AppSettingsInstallerParameters.ProjectName, AppSettingsInstallerParameters.ServerInfo.EnvironmentName,
            AppSettingsInstallerParameters.AppSettingsEncodedJsonFileName, UseConsole);
        if (!await installParametersAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName} parameters file is not updated", projectName);
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        //, AppSettingsInstallerParameters.ProjectName
        var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
            AppSettingsInstallerParameters.WebAgentForCheck, AppSettingsInstallerParameters.ProxySettings,
            appSettingsVersion, 10, UseConsole);

        if (await checkParametersVersionAction.Run(cancellationToken))
            return true;

        _logger.LogError("project {projectName} parameters file check failed", projectName);
        return false;
    }
}