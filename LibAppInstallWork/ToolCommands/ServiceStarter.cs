//Created by ProjectMainClassCreator at 5/10/2021 16:03:33

using System;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStarter : ToolCommand
{
    private const string ActionName = "Starting Service";
    private const string ActionDescription = "Starting Service";
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStarter(ILogger logger, ServiceStartStopParameters parameters, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, ActionDescription)
    {
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

        Logger.LogInformation("Try to start service {projectName}/{environmentName}...", projectName, environmentName);

        if (string.IsNullOrWhiteSpace(projectName))
        {
            Logger.LogError("Service Name not specified");
            return false;
        }

        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabric.CreateProjectsApiClient(Logger, _parameters.WebAgentForInstall,
                _parameters.InstallFolder);

        if (agentClient is null)
        {
            Logger.LogError("agentClient does not created. Service {projectName}/{environmentName} can not started",
                projectName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაშვების მცდელობა.
        var startServiceResult = await agentClient.StartService(_parameters.ProjectName, _parameters.EnvironmentName,
            CancellationToken.None);
        if (startServiceResult.IsSome)
        {
            Logger.LogError("Service {projectName}/{environmentName} can not started", projectName, environmentName);
            return false;
        }

        if (agentClient is IDisposable disposable)
            disposable.Dispose();

        Logger.LogInformation("Service Started");

        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction =
            new(Logger, _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!await checkParametersVersionAction.Run(cancellationToken))
            Logger.LogError("Service {projectName}/{environmentName} parameters file check failed", projectName,
                environmentName);


        //შევამოწმოთ გაშვებული პროგრამის ვერსია
        CheckProgramVersionAction checkProgramVersionAction =
            new(Logger, _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
            Logger.LogError("Service {projectName}/{environmentName} version check failed", projectName,
                environmentName);

        return true;
    }
}