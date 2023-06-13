using CliParameters;
using Installer.Actions;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class AppSettingsInstaller : ToolCommand
{
    private const string ActionName = "Install Application Settings";

    private const string ActionDescription =
        "This tool will Download latest parameters file from exchange server, then will update parameters file, and check if parameters updated";

    public AppSettingsInstaller(ILogger logger, bool useConsole, AppSettingsInstallerParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters,
        parametersManager, ActionDescription)
    {
    }

    private AppSettingsInstallerParameters AppSettingsInstallerParameters => (AppSettingsInstallerParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        //1. მოვქაჩოთ ფაილსაცავში არსებული უახლესი პარამეტრების ფაილის შიგთავსი.
        GetLatestParametersFileBodyAction getLatestParametersFileBodyAction = new(Logger, true,
            AppSettingsInstallerParameters.FileStorageForDownload, AppSettingsInstallerParameters.ProjectName,
            AppSettingsInstallerParameters.ServerName, AppSettingsInstallerParameters.ParametersFileDateMask,
            AppSettingsInstallerParameters.ParametersFileExtension);
        var result = getLatestParametersFileBodyAction.Run();
        var appSettingsVersion = getLatestParametersFileBodyAction.AppSettingsVersion;
        if (!result || string.IsNullOrWhiteSpace(appSettingsVersion))
        {
            Logger.LogError("Parameters version does not detected");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        InstallParametersAction installParametersAction = new(Logger, UseConsole,
            AppSettingsInstallerParameters.ParametersFileDateMask,
            AppSettingsInstallerParameters.ParametersFileExtension,
            AppSettingsInstallerParameters.InstallerBaseParameters, AppSettingsInstallerParameters.FileStorageForUpload,
            AppSettingsInstallerParameters.ProjectName, AppSettingsInstallerParameters.ServiceName,
            AppSettingsInstallerParameters.AppSettingsEncodedJsonFileName);
        if (!installParametersAction.Run())
        {
            Logger.LogError($"project {AppSettingsInstallerParameters.ProjectName} parameters file is not updated");
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        //, AppSettingsInstallerParameters.ProjectName
        CheckParametersVersionAction checkParametersVersionAction = new(Logger, UseConsole,
            AppSettingsInstallerParameters.WebAgentForCheck, AppSettingsInstallerParameters.ProxySettings,
            appSettingsVersion);

        if (checkParametersVersionAction.Run())
            return true;

        Logger.LogError($"project {AppSettingsInstallerParameters.ProjectName} parameters file check failed");
        return false;
    }
}