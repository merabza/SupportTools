//Created by ProjectMainClassCreator at 5/10/2021 16:04:08

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LanguageExt;
using LibAppInstallWork.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared.Errors;
using ToolsManagement.Installer.ProjectManagers;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStopperToolCommand : ToolCommand
{
    private const string ActionName = "Stop Service";
    private const string ActionDescription = "Stop Service";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStopperToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
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
            _logger.LogInformation("Try to stop service {ProjectName}/{EnvironmentName}...", projectName,
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
            _logger.LogError("agentClient does not created. Service {ProjectName}/{EnvironmentName} can not be stopped",
                projectName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაჩერების მცდელობა.
        Option<Err[]> stopServiceResult =
            await projectManager.StopService(projectName, environmentName, cancellationToken);
        if (stopServiceResult.IsSome)
        {
            _logger.LogError("Service {ProjectName}/{EnvironmentName} can not be stopped", projectName,
                environmentName);
            return false;
        }

        _logger.LogInformation("Service Stopped");
        return true;
    }
}
