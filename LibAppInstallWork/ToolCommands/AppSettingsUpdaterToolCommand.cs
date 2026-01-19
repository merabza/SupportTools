//Created by ProjectMainClassCreator at 1/11/2021 20:04:36

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class AppSettingsUpdaterToolCommand : ToolCommand
{
    private const string ActionName = "Update Settings";

    private const string ActionDescription =
        "this tool will crate new encoded parameters file, then will Install parameters file server side adn will chek that parameters updated";

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _logger;

    public AppSettingsUpdaterToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        AppSettingsUpdaterParameters parameters, IParametersManager parametersManager, bool useConsole) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private AppSettingsUpdaterParameters AppSettingsUpdaterParameters => (AppSettingsUpdaterParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var appSettingsEncoderParameters = AppSettingsUpdaterParameters.AppSettingsEncoderParameters;

        //1. დავამზადოთ პარამეტრების ფაილი დაშიფრული და ავტვირთოთ ფაილსაცავში პარამეტრების ფაილის შიგთავსი.
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(_logger,
            appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
            appSettingsEncoderParameters.AppSettingsJsonSourceFileName,
            appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, appSettingsEncoderParameters.KeyPart1,
            appSettingsEncoderParameters.KeyPart2, appSettingsEncoderParameters.ProjectName,
            appSettingsEncoderParameters.ServerInfo, appSettingsEncoderParameters.DateMask,
            appSettingsEncoderParameters.ParametersFileExtension, appSettingsEncoderParameters.FileStorageForExchange,
            appSettingsEncoderParameters.ExchangeSmartSchema);
        var result = await encodeParametersAndUploadAction.Run(cancellationToken);
        var encodedJson = encodeParametersAndUploadAction.EncodedJsonContent;
        var checkForVersion = encodeParametersAndUploadAction.AppSettingsVersion;

        if (!result || string.IsNullOrWhiteSpace(encodedJson) || string.IsNullOrWhiteSpace(checkForVersion))
        {
            _logger.LogError("Cannot encode parameters");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        var installParametersAction = new InstallParametersAction(_logger, _httpClientFactory,
            AppSettingsUpdaterParameters.ParametersFileDateMask, AppSettingsUpdaterParameters.ParametersFileExtension,
            AppSettingsUpdaterParameters.InstallerBaseParameters, AppSettingsUpdaterParameters.FileStorageForUpload,
            AppSettingsUpdaterParameters.ProjectName, AppSettingsUpdaterParameters.EnvironmentName,
            AppSettingsUpdaterParameters.AppSettingsEncoderParameters.AppSettingsEncodedJsonFileName, UseConsole);
        var projectName = AppSettingsUpdaterParameters.ProjectName;
        if (!await installParametersAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName} parameters file is not updated", projectName);
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
            AppSettingsUpdaterParameters.WebAgentForCheck, AppSettingsUpdaterParameters.ProxySettings, checkForVersion,
            10, UseConsole);

        if (await checkParametersVersionAction.Run(cancellationToken))
            return true;

        _logger.LogError("project {projectName} parameters file check failed", projectName);
        return false;
    }
}