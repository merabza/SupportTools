//Created by ProjectMainClassCreator at 5/10/2021 16:04:08

using System;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStopper : ToolCommand
{
    private const string ActionName = "Stop Service";
    private const string ActionDescription = "Stop Service";
    private readonly ILogger _logger;
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStopper(ILogger logger, ServiceStartStopParameters parameters, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, ActionDescription)
    {
        _logger = logger;
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
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
        var agentClient =
            ProjectsAgentClientsFabric.CreateProjectsApiClient(_logger, _parameters.WebAgentForInstall,
                _parameters.InstallFolder);

        if (agentClient is null)
        {
            _logger.LogError("agentClient does not created. Service {projectName}/{environmentName} can not be stopped",
                projectName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაჩერების მცდელობა.
        var stopServiceResult = await agentClient.StopService(projectName, environmentName, cancellationToken);
        if (stopServiceResult.IsSome)
        {
            _logger.LogError("Service {projectName}/{environmentName} can not be stopped", projectName,
                environmentName);
            return false;
        }

        if (agentClient is IDisposable disposable)
            disposable.Dispose();

        _logger.LogInformation("Service Stopped");
        return true;
    }
}