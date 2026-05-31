//Created by ProjectMainClassCreator at 1/11/2021 20:04:36

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppInstallWork.ToolActions;
using LibAppInstallWork.ToolCommands.AppSettingsEncoder;
using LibAppInstallWork.ToolCommands.AppSettingsPreparer;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands.AppSettingsUpdater;

public sealed class AppSettingsUpdaterToolCommand : ToolCommand
{
    private const string ActionName = "Update Settings";

    private const string ActionDescription =
        "this tool will crate new encoded parameters file, then will Install parameters file server side adn will chek that parameters updated";

    private readonly string _appName;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ILogger _logger;

    public AppSettingsUpdaterToolCommand(string appName, ILogger logger, IHttpClientFactory httpClientFactory,
        AppSettingsUpdaterParameters parameters, IParametersManager parametersManager, bool useConsole) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription, useConsole)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    private AppSettingsUpdaterParameters AppSettingsUpdaterParameters => (AppSettingsUpdaterParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var supportToolsParameters = (SupportToolsParameters)_parametersManager.Parameters;

        AppSettingsPreparerParameters appSettingsPreparerParameters =
            AppSettingsPreparerParameters.Create(supportToolsParameters, AppSettingsUpdaterParameters.ProjectName,
                AppSettingsUpdaterParameters.ServerInfo);

        var appSettingsFileName = "appsettings.json";

        if (appSettingsPreparerParameters is null)
        {
            StShared.WriteErrorLine($"AppSettingsPreparerParameters can not be prepared for Project {AppSettingsUpdaterParameters.ProjectName}", true, _logger);
            return false;
        }

        AppSettingsEncoderParameters appSettingsEncoderParameters =
            AppSettingsEncoderParameters.Create(supportToolsParameters, AppSettingsUpdaterParameters.ProjectName,
                AppSettingsUpdaterParameters.ServerInfo);

        string? checkForVersion;
        string? appSettingsContent;
        bool result;
        if (appSettingsEncoderParameters != null)
        {
            //1. დავამზადოთ პარამეტრების ფაილი დაშიფრული და ავტვირთოთ ფაილსაცავში პარამეტრების ფაილის შიგთავსი.
            var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
                appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
                appSettingsPreparerParameters.AppSettingsJsonSourceFileName,
                appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, appSettingsEncoderParameters.KeyPart1,
                appSettingsEncoderParameters.KeyPart2, appSettingsPreparerParameters.ProjectName,
                appSettingsPreparerParameters.ServerInfo, appSettingsPreparerParameters.DateMask,
                appSettingsPreparerParameters.ParametersFileExtension,
                appSettingsPreparerParameters.FileStorageForExchange, appSettingsPreparerParameters.ExchangeSmartSchema);
            result = await encodeParametersAndUploadAction.Run(cancellationToken);

            appSettingsFileName = Path.GetFileName(appSettingsEncoderParameters.AppSettingsEncodedJsonFileName);
            appSettingsContent = encodeParametersAndUploadAction.EncodedJsonContent;
            checkForVersion = encodeParametersAndUploadAction.AppSettingsVersion;
        }
        else
        {
            var prepareParametersAndUploadAction = new PrepareParametersAndUploadAction(_logger,
                appSettingsPreparerParameters.AppSettingsJsonSourceFileName, appSettingsPreparerParameters.ProjectName,
                appSettingsPreparerParameters.ServerInfo, appSettingsPreparerParameters.DateMask,
                appSettingsPreparerParameters.ParametersFileExtension,
                appSettingsPreparerParameters.FileStorageForExchange,
                appSettingsPreparerParameters.ExchangeSmartSchema);
            result = await prepareParametersAndUploadAction.Run(cancellationToken);
            appSettingsContent = prepareParametersAndUploadAction.PreparedJsonContent;
            checkForVersion = prepareParametersAndUploadAction.AppSettingsVersion;
        }

        if (!result || string.IsNullOrWhiteSpace(appSettingsContent) || string.IsNullOrWhiteSpace(checkForVersion))
        {
            _logger.LogError("Cannot encode parameters");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        var installParametersAction = new InstallParametersAction(_appName, _logger, _httpClientFactory,
            AppSettingsUpdaterParameters.ParametersFileDateMask, AppSettingsUpdaterParameters.ParametersFileExtension,
            AppSettingsUpdaterParameters.InstallerBaseParameters, AppSettingsUpdaterParameters.FileStorageForUpload,
            AppSettingsUpdaterParameters.ProjectName, AppSettingsUpdaterParameters.EnvironmentName,
            appSettingsFileName, UseConsole);
        string projectName = AppSettingsUpdaterParameters.ProjectName;
        if (!await installParametersAction.Run(cancellationToken))
        {
            _logger.LogError("project {ProjectName} parameters file is not updated", projectName);
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
            AppSettingsUpdaterParameters.WebAgentForCheck, AppSettingsUpdaterParameters.ProxySettings, checkForVersion,
            10, UseConsole);

        if (await checkParametersVersionAction.Run(cancellationToken))
        {
            return true;
        }

        _logger.LogError("project {ProjectName} parameters file check failed", projectName);
        return false;
    }
}
