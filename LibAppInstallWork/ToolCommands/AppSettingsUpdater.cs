//Created by ProjectMainClassCreator at 1/11/2021 20:04:36

using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class AppSettingsUpdater : ToolCommand
{
    private const string ActionName = "Update Settings";

    private const string ActionDescription =
        "this tool will crate new encoded parameters file, then will Install parameters file server side adn will chek that parameters updated";

    public AppSettingsUpdater(ILogger logger, bool useConsole, AppSettingsUpdaterParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        ActionDescription)
    {
    }

    private AppSettingsUpdaterParameters AppSettingsUpdaterParameters => (AppSettingsUpdaterParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var appSettingsEncoderParameters =
            AppSettingsUpdaterParameters.AppSettingsEncoderParameters;

        //1. დავამზადოთ პარამეტრების ფაილი დაშიფრული და ავტვირთოთ ფაილსაცავში პარამეტრების ფაილის შიგთავსი.
        var encodeParametersAndUploadAction = new EncodeParametersAndUploadAction(Logger,
            UseConsole, appSettingsEncoderParameters.AppSetEnKeysJsonFileName,
            appSettingsEncoderParameters.AppSettingsJsonSourceFileName,
            appSettingsEncoderParameters.AppSettingsEncodedJsonFileName, appSettingsEncoderParameters.KeyPart1,
            appSettingsEncoderParameters.KeyPart2, appSettingsEncoderParameters.ProjectName,
            appSettingsEncoderParameters.ServerInfo, appSettingsEncoderParameters.DateMask,
            appSettingsEncoderParameters.ParametersFileExtension, appSettingsEncoderParameters.FileStorageForExchange,
            appSettingsEncoderParameters.ExchangeSmartSchema);
        var result = encodeParametersAndUploadAction.Run();
        var encodedJson = encodeParametersAndUploadAction.EncodedJsonContent;
        var checkForVersion = encodeParametersAndUploadAction.AppSettingsVersion;

        if (!result || string.IsNullOrWhiteSpace(encodedJson) || string.IsNullOrWhiteSpace(checkForVersion))
        {
            Logger.LogError("Cannot encode parameters");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        var installParametersAction = new InstallParametersAction(Logger, UseConsole,
            AppSettingsUpdaterParameters.ParametersFileDateMask, AppSettingsUpdaterParameters.ParametersFileExtension,
            AppSettingsUpdaterParameters.InstallerBaseParameters, AppSettingsUpdaterParameters.FileStorageForUpload,
            AppSettingsUpdaterParameters.ProjectName, AppSettingsUpdaterParameters.EnvironmentName,
            AppSettingsUpdaterParameters.ServiceName,
            AppSettingsUpdaterParameters.AppSettingsEncoderParameters.AppSettingsEncodedJsonFileName);

        if (!installParametersAction.Run())
        {
            Logger.LogError($"project {AppSettingsUpdaterParameters.ProjectName} parameters file is not updated");
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        var checkParametersVersionAction = new CheckParametersVersionAction(Logger, UseConsole,
            AppSettingsUpdaterParameters.WebAgentForCheck, AppSettingsUpdaterParameters.ProxySettings, checkForVersion);

        if (checkParametersVersionAction.Run())
            return true;

        Logger.LogError($"project {AppSettingsUpdaterParameters.ProjectName} parameters file check failed");
        return false;
    }
}