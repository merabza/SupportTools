//Created by ProjectMainClassCreator at 1/11/2021 20:04:36

using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class AppSettingsUpdater : ToolCommand
{
    private const string ActionName = "Update Settings";

    private const string ActionDescription =
        "this tool will crate new encoded parameters file, then will Install parameters file server side adn will chek that parameters updated";

    public AppSettingsUpdater(ILogger logger, AppSettingsUpdaterParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
    }

    private AppSettingsUpdaterParameters AppSettingsUpdaterParameters => (AppSettingsUpdaterParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var appSettingsEncoderParameters =
            AppSettingsUpdaterParameters.AppSettingsEncoderParameters;

        //1. დავამზადოთ პარამეტრების ფაილი დაშიფრული და ავტვირთოთ ფაილსაცავში პარამეტრების ფაილის შიგთავსი.
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger,
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
            Logger.LogError("Cannot encode parameters");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        var installParametersAction = new InstallParametersAction(Logger,
            AppSettingsUpdaterParameters.ParametersFileDateMask, AppSettingsUpdaterParameters.ParametersFileExtension,
            AppSettingsUpdaterParameters.InstallerBaseParameters, AppSettingsUpdaterParameters.FileStorageForUpload,
            AppSettingsUpdaterParameters.ProjectName, AppSettingsUpdaterParameters.EnvironmentName,
            AppSettingsUpdaterParameters.AppSettingsEncoderParameters.AppSettingsEncodedJsonFileName);
        var projectName = AppSettingsUpdaterParameters.ProjectName;
        if (!await installParametersAction.Run(cancellationToken))
        {
            Logger.LogError("project {projectName} parameters file is not updated", projectName);
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        var checkParametersVersionAction = new CheckParametersVersionAction(Logger,
            AppSettingsUpdaterParameters.WebAgentForCheck, AppSettingsUpdaterParameters.ProxySettings, checkForVersion);

        if (await checkParametersVersionAction.Run(cancellationToken))
            return true;

        Logger.LogError("project {projectName} parameters file check failed", projectName);
        return false;
    }
}