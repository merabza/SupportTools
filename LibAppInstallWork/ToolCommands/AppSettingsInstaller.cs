using CliParameters;
using Installer.Actions;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class AppSettingsInstaller : ToolCommand
{
    private readonly bool _useConsole;
    private const string ActionName = "Install Application Settings";

    private const string ActionDescription =
        "This tool will Download latest parameters file from exchange server, then will update parameters file, and check if parameters updated";

    public AppSettingsInstaller(ILogger logger, bool useConsole, AppSettingsInstallerParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters,
        parametersManager, ActionDescription)
    {
        _useConsole = useConsole;
    }

    private AppSettingsInstallerParameters AppSettingsInstallerParameters => (AppSettingsInstallerParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        if (string.IsNullOrWhiteSpace(AppSettingsInstallerParameters.ServerInfo.ServerName))
        {
            Logger.LogError("Server name is not specified");
            return false;
        }


        if (string.IsNullOrWhiteSpace(AppSettingsInstallerParameters.ServerInfo.EnvironmentName))
        {
            Logger.LogError("Environment name is not specified");
            return false;
        }

        var projectName = AppSettingsInstallerParameters.ProjectName;
        //1. მოვქაჩოთ ფაილსაცავში არსებული უახლესი პარამეტრების ფაილის შიგთავსი.
        GetLatestParametersFileBodyAction getLatestParametersFileBodyAction = new(Logger, _useConsole,
            AppSettingsInstallerParameters.FileStorageForDownload, AppSettingsInstallerParameters.ProjectName,
            AppSettingsInstallerParameters.ServerInfo.ServerName,
            AppSettingsInstallerParameters.ServerInfo.EnvironmentName,
            AppSettingsInstallerParameters.ParametersFileDateMask,
            AppSettingsInstallerParameters.ParametersFileExtension, null, null);
        var result = getLatestParametersFileBodyAction.Run();
        var appSettingsVersion = getLatestParametersFileBodyAction.AppSettingsVersion;
        if (!result || string.IsNullOrWhiteSpace(appSettingsVersion))
        {
            Logger.LogError("Parameters version does not detected");
            return false;
        }

        //2. მოვსინჯოთ პარამეტრების ფაილის დაინსტალირება ან განახლება პროგრამის მხარეს.
        InstallParametersAction installParametersAction = new(Logger,
            AppSettingsInstallerParameters.ParametersFileDateMask,
            AppSettingsInstallerParameters.ParametersFileExtension,
            AppSettingsInstallerParameters.InstallerBaseParameters, AppSettingsInstallerParameters.FileStorageForUpload,
            AppSettingsInstallerParameters.ProjectName, AppSettingsInstallerParameters.ServerInfo.EnvironmentName,
            AppSettingsInstallerParameters.ServiceName, AppSettingsInstallerParameters.AppSettingsEncodedJsonFileName);
        if (!installParametersAction.Run())
        {
            Logger.LogError("project {projectName} parameters file is not updated", projectName);
            return false;
        }

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
        //, AppSettingsInstallerParameters.ProjectName
        CheckParametersVersionAction checkParametersVersionAction = new(Logger,
            AppSettingsInstallerParameters.WebAgentForCheck, AppSettingsInstallerParameters.ProxySettings,
            appSettingsVersion);

        if (checkParametersVersionAction.Run())
            return true;

        Logger.LogError("project {projectName} parameters file check failed", projectName);
        return false;
    }
}