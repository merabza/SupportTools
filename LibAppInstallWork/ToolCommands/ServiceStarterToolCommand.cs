//Created by ProjectMainClassCreator at 5/10/2021 16:03:33

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LanguageExt;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.Installer.ProjectManagers;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStarterToolCommand : ToolCommand
{
    private const string ActionName = "Starting Service";
    private const string ActionDescription = "Starting Service";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStarterToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ServiceStartStopParameters parameters, IParametersManager parametersManager, bool useConsole) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        string projectName = _parameters.ProjectName;
        string environmentName = _parameters.EnvironmentName;

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Try to start service {ProjectName}/{EnvironmentName}...", projectName,
                environmentName);
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            _logger.LogError("Service Name not specified");
            return false;
        }

        //კლიენტის შექმნა
        IProjectsManager? projectManager = ProjectsManagersFactory.CreateProjectsManager(_logger, _httpClientFactory,
            _parameters.WebAgentForInstall, _parameters.InstallFolder, UseConsole);

        if (projectManager is null)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogError(
                    "agentClient does not created. Service {ProjectName}/{EnvironmentName} can not started",
                    projectName, environmentName);
            }

            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაშვების მცდელობა.
        Option<Err[]> startServiceResult = await projectManager.StartService(_parameters.ProjectName,
            _parameters.EnvironmentName, CancellationToken.None);
        if (startServiceResult.IsSome)
        {
            _logger.LogError("Service {ProjectName}/{EnvironmentName} can not started", projectName, environmentName);
            return false;
        }

        _logger.LogInformation("Service Started");

        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
            _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1, UseConsole);
        if (!await checkParametersVersionAction.Run(cancellationToken))
        {
            _logger.LogError("Service {ProjectName}/{EnvironmentName} parameters file check failed", projectName,
                environmentName);
        }

        //შევამოწმოთ გაშვებული პროგრამის ვერსია
        var checkProgramVersionAction = new CheckProgramVersionAction(_logger, _httpClientFactory,
            _parameters.WebAgentForCheck, _parameters.ProxySettings, null, UseConsole, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
        {
            _logger.LogError("Service {ProjectName}/{EnvironmentName} version check failed", projectName,
                environmentName);
        }

        return true;
    }
}
