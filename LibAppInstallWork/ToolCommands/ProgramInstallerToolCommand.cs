//Created by ProjectMainClassCreator at 12/22/2020 19:46:54

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using Installer.ToolActions;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramInstallerToolCommand : ToolCommand
{
    private const string ActionName = "Installing Program";
    private const string ActionDescription = "Installing Program";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly bool _useConsole;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramInstallerToolCommand(ILogger logger, IHttpClientFactory httpClientFactory, bool useConsole,
        IParameters parameters, IParametersManager parametersManager) : base(logger, ActionName, parameters,
        parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _useConsole = useConsole;
    }

    private ProgramInstallerParameters Parameters => (ProgramInstallerParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Parameters.ServerInfo.ServerName))
        {
            _logger.LogError("Server name is not specified");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Parameters.ServerInfo.EnvironmentName))
        {
            _logger.LogError("Environment name is not specified");
            return false;
        }

        if (!Parameters.IsService)
        {
            //3. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
            var installProgramAction = new InstallProgramAction(_logger, _httpClientFactory,
                Parameters.InstallerBaseParameters, Parameters.ProgramArchiveDateMask,
                Parameters.ProgramArchiveExtension, Parameters.ParametersFileDateMask,
                Parameters.ParametersFileExtension, Parameters.FileStorageForExchange, Parameters.ProjectName,
                Parameters.ServerInfo.EnvironmentName, _useConsole);

            return await installProgramAction.Run(cancellationToken);
        }

        string? appSettingsVersion = null;
        if (!string.IsNullOrWhiteSpace(Parameters.AppSettingsJsonSourceFileName) ||
            !string.IsNullOrWhiteSpace(Parameters.EncodedJsonFileName))
        {
            //1. მოვქაჩოთ ფაილსაცავში არსებული უახლესი პარამეტრების ფაილის შიგთავსი.
            var getLatestParametersFileBodyAction = new GetLatestParametersFileBodyAction(_logger, _useConsole,
                Parameters.FileStorageForExchange, Parameters.ProjectName, Parameters.ServerInfo.ServerName,
                Parameters.ServerInfo.EnvironmentName, Parameters.ParametersFileDateMask,
                Parameters.ParametersFileExtension, null, null);
            var result = await getLatestParametersFileBodyAction.Run(cancellationToken);

            appSettingsVersion = getLatestParametersFileBodyAction.AppSettingsVersion;

            if (!result || string.IsNullOrWhiteSpace(appSettingsVersion))
            {
                _logger.LogError("Parameters version does not detected");
                return false;
            }
        }

        //2. გავუშვათ ინსტალაციის პროცესი, ამ პროცესის დასრულების შემდეგ უნდა მივიღოთ დაინსტალირებისას დადგენილი პროგრამის ვერსია.
        var projectName = Parameters.ProjectName;
        var environmentName = Parameters.ServerInfo.EnvironmentName;
        var installServiceAction = new InstallServiceAction(_logger, _httpClientFactory,
            Parameters.InstallerBaseParameters, Parameters.ProgramArchiveDateMask, Parameters.ProgramArchiveExtension,
            Parameters.ParametersFileDateMask, Parameters.ParametersFileExtension, Parameters.FileStorageForExchange,
            projectName, Parameters.ServerInfo.EnvironmentName, Parameters.ServiceUserName,
            Parameters.EncodedJsonFileName, Parameters.ServiceDescriptionSignature, Parameters.ProjectDescription,
            UseConsole);
        if (!await installServiceAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName}/{environmentName} was not updated", projectName, environmentName);
            return false;
        }

        var installingProgramVersion = installServiceAction.InstallingProgramVersion;

        //3. შევამოწმოთ, რომ გაშვებული პროგრამის ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით//, projectName
        var checkProgramVersionAction = new CheckProgramVersionAction(_logger, _httpClientFactory,
            Parameters.WebAgentForCheck, Parameters.ProxySettings, installingProgramVersion, UseConsole);
        if (!await checkProgramVersionAction.Run(cancellationToken))
        {
            _logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
                environmentName);
            return false;
        }

        if (appSettingsVersion != null)
        {
            //4. შევამოწმოთ, რომ გაშვებული პროგრამის პარამეტრების ვერსია ემთხვევა იმას, რის დაინსტალირებასაც ვცდილობდით
            var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
                Parameters.WebAgentForCheck, Parameters.ProxySettings, appSettingsVersion, 10, UseConsole);

            if (await checkParametersVersionAction.Run(cancellationToken))
                return true;
        }

        _logger.LogError("project {projectName}/{environmentName} parameters file check failed", projectName,
            environmentName);
        return false;
    }
}