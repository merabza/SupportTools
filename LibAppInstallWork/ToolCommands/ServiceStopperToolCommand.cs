//Created by ProjectMainClassCreator at 5/10/2021 16:04:08

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStopperToolCommand : ToolCommand
{
    private const string ActionName = "Stop Service";
    private const string ActionDescription = "Stop Service";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStopperToolCommand(ILogger logger, IHttpClientFactory httpClientFactory, ServiceStartStopParameters parameters,
        IParametersManager parametersManager, bool useConsole) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription, useConsole)
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
        var projectName = _parameters.ProjectName;
        var environmentName = _parameters.EnvironmentName;

        _logger.LogInformation("Try to stop service {projectName}/{environmentName}...", projectName, environmentName);

        if (string.IsNullOrWhiteSpace(projectName))
        {
            _logger.LogError("Service Name not specified");
            return false;
        }

        //კლიენტის შექმნა
        var projectManager = ProjectsManagersFabric.CreateProjectsManager(_logger, _httpClientFactory,
            _parameters.WebAgentForInstall, _parameters.InstallFolder, UseConsole);

        if (projectManager is null)
        {
            _logger.LogError("agentClient does not created. Service {projectName}/{environmentName} can not be stopped",
                projectName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაჩერების მცდელობა.
        var stopServiceResult = await projectManager.StopService(projectName, environmentName, cancellationToken);
        if (stopServiceResult.IsSome)
        {
            _logger.LogError("Service {projectName}/{environmentName} can not be stopped", projectName,
                environmentName);
            return false;
        }

        _logger.LogInformation("Service Stopped");
        return true;
    }
}