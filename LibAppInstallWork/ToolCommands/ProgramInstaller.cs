//Created by ProjectMainClassCreator at 12/22/2020 19:46:54

using System.Threading;
using System.Threading.Tasks;
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
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramInstaller(ILogger logger, bool useConsole, IParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters,
        parametersManager, ActionDescription)
    {
        _useConsole = useConsole;
    }

    private ProgramInstallerParameters Parameters => (ProgramInstallerParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Parameters.ServerInfo.ServerName))
        {
            Logger.LogError("Server name is not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.ServerInfo.EnvironmentName))
        {
            Logger.LogError("Environment name is not specified");
            return false;
        }

        if (!Parameters.IsService)
        {
            //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
            var installProgramAction = new InstallProgramAction(Logger, Parameters.InstallerBaseParameters,
                Parameters.ProgramArchiveDateMask, Parameters.ProgramArchiveExtension,
                Parameters.ParametersFileDateMask, Parameters.ParametersFileExtension,
                Parameters.FileStorageForExchange, Parameters.ProjectName, Parameters.ServerInfo.EnvironmentName);

            return await installProgramAction.Run(cancellationToken);
        }



        string? appSettingsVersion = null;
        if (!string.IsNullOrWhiteSpace(Parameters.AppSettingsJsonSourceFileName) ||
            !string.IsNullOrWhiteSpace(Parameters.EncodedJsonFileName))
        {

            //1. მოვქაჩოთ ფაილსაცავში არსებული უახლესი პარამეტრების ფაილის შიგთავსი.
            var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(Logger, _useConsole,
                Parameters.FileStorageForExchange, Parameters.ProjectName, Parameters.ServerInfo.ServerName,
                Parameters.ServerInfo.EnvironmentName, Parameters.ParametersFileDateMask,
                Parameters.ParametersFileExtension, null, null);
            var result = await getLatestParametersFileBodyAction.Run(cancellationToken);

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
        var installServiceAction = new InstallServiceAction(Logger, Parameters.InstallerBaseParameters,
            Parameters.ProgramArchiveDateMask, Parameters.ProgramArchiveExtension, Parameters.ParametersFileDateMask,
            Parameters.ParametersFileExtension, Parameters.FileStorageForExchange, projectName,
            Parameters.ServerInfo.EnvironmentName, Parameters.ServiceUserName, Parameters.EncodedJsonFileName,
            Parameters.ServiceDescriptionSignature, Parameters.ProjectDescription);
        if (!await installServiceAction.Run(cancellationToken))
        {
            Logger.LogError("project {projectName}/{environmentName} was not updated", projectName, environmentName);
            return false;
        }

        var installingProgramVersion = installServiceAction.InstallingProgramVersion;

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(Logger, Parameters.WebAgentForCheck,
            Parameters.ProxySettings, installingProgramVersion);
        if (!await checkProgramVersionAction.Run(cancellationToken))
        {
            Logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
                environmentName);
            return false;
        }

        if (appSettingsVersion != null)
        {
            //4. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
            var checkParametersVersionAction = new CheckParametersVersionAction(Logger, Parameters.WebAgentForCheck,
                Parameters.ProxySettings, appSettingsVersion);

            if (await checkParametersVersionAction.Run(cancellationToken))
                return true;
        }


        Logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}