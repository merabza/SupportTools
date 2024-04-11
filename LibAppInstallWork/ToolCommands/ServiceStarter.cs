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
        if (!string.IsNullOrWhiteSpace(_parameters.ServiceName))
            return true;

        Logger.LogError("Service Name not specified");
        return false;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var serviceName = _parameters.ServiceName;
        var environmentName = _parameters.EnvironmentName;

        Logger.LogInformation("Try to start service {serviceName}/{environmentName}...", serviceName, environmentName);

        if (string.IsNullOrWhiteSpace(serviceName))
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
            Logger.LogError("agentClient does not created. Service {serviceName}/{environmentName} can not started",
                serviceName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაშვების მცდელობა.
        var startServiceResult = await agentClient.StartService(_parameters.ServiceName, _parameters.EnvironmentName,
            CancellationToken.None);
        if (startServiceResult.IsSome)
        {
            Logger.LogError("Service {serviceName}/{environmentName} can not started", serviceName, environmentName);
            return false;
        }
        
        if (agentClient is IDisposable disposable)
            disposable.Dispose();

        Logger.LogInformation("Service Started");

        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction =
            new(Logger, _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!await checkParametersVersionAction.Run(cancellationToken))
            Logger.LogError("Service {serviceName}/{environmentName} parameters file check failed", serviceName,
                environmentName);


        //შევამოწმოთ გაშვებული პროგრამის ვერსია ServiceStartParameters.ServiceName, 
        CheckProgramVersionAction checkProgramVersionAction =
            new(Logger, _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
            Logger.LogError("Service {serviceName}/{environmentName} version check failed", serviceName,
                environmentName);

        return true;
    }
}