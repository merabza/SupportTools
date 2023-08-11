//Created by ProjectMainClassCreator at 12/22/2020 19:46:54

using CliParameters;
using Installer.Actions;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramInstaller : ToolCommand
{
    private const string ActionName = "Installing Program";
    private const string ActionDescription = "Installing Program";

    public ProgramInstaller(ILogger logger, bool useConsole, IParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters,
        parametersManager, ActionDescription)
    {
    }

    private ProgramInstallerParameters Parameters => (ProgramInstallerParameters)Par;

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        string? appSettingsVersion = null;

        if (string.IsNullOrWhiteSpace(Parameters.ServerInfo.EnvironmentName))
        {
            Logger.LogError("Environment name is not specified");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Parameters.AppSettingsJsonSourceFileName) ||
            !string.IsNullOrWhiteSpace(Parameters.EncodedJsonFileName))
        {
            if (string.IsNullOrWhiteSpace(Parameters.ServerInfo.ServerName))
            {
                Logger.LogError("Server name is not specified");
                return false;
            }

            //1. მოვქაჩოთ ფაილსაცავში არსებული უახლესი პარამეტრების ფაილის შიგთავსი.
            var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(Logger, true,
                Parameters.FileStorageForExchange, Parameters.ProjectName, Parameters.ServerInfo.ServerName,
                Parameters.ServerInfo.EnvironmentName, Parameters.ParametersFileDateMask,
                Parameters.ParametersFileExtension);
            var result = getLatestParametersFileBodyAction.Run();

            appSettingsVersion = getLatestParametersFileBodyAction.AppSettingsVersion;

            if (!result || string.IsNullOrWhiteSpace(appSettingsVersion))
            {
                Logger.LogError("Parameters version does not detected");
                return false;
            }
        }

        //2. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var projectName = Parameters.ProjectName;
        var environmentName = Parameters.ServerInfo.EnvironmentName;
        var installProgramAction = new InstallServiceAction(Logger, UseConsole, Parameters.InstallerBaseParameters,
            Parameters.ProgramArchiveDateMask, Parameters.ProgramArchiveExtension, Parameters.ParametersFileDateMask,
            Parameters.ParametersFileExtension, Parameters.FileStorageForExchange, projectName,
            Parameters.ServerInfo.EnvironmentName, Parameters.ServiceName, Parameters.ServiceUserName,
            Parameters.EncodedJsonFileName);
        if (!installProgramAction.Run())
        {
            Logger.LogError("project {projectName}/{environmentName} was not updated", projectName, environmentName);
            return false;
        }

        var installingProgramVersion = installProgramAction.InstallingProgramVersion;

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(Logger, UseConsole, Parameters.WebAgentForCheck,
            Parameters.ProxySettings, installingProgramVersion);
        if (!checkProgramVersionAction.Run())
        {
            Logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
                environmentName);
            return false;
        }

        if (appSettingsVersion != null)
        {
            //4. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
            var checkParametersVersionAction = new CheckParametersVersionAction(Logger, UseConsole,
                Parameters.WebAgentForCheck, Parameters.ProxySettings, appSettingsVersion);

            if (checkParametersVersionAction.Run())
                return true;
        }


        Logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}