//Created by ProjectMainClassCreator at 5/10/2021 16:04:08

using System.Threading;
using CliParameters;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStopper : ToolCommand
{
    private const string ActionName = "Stop Service";
    private const string ActionDescription = "Stop Service";
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStopper(ILogger logger, ServiceStartStopParameters parameters, IParametersManager parametersManager) :
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

    protected override bool RunAction()
    {
        var serviceName = _parameters.ServiceName;
        var environmentName = _parameters.EnvironmentName;

        Logger.LogInformation("Try to stop service {serviceName}/{environmentName}...", serviceName, environmentName);

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
            Logger.LogError("agentClient does not created. Service {serviceName}/{environmentName} can not be stopped",
                serviceName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაჩერების მცდელობა.
        if (!agentClient.StopService(serviceName, environmentName, CancellationToken.None).Result)
        {
            Logger.LogError("Service {serviceName}/{environmentName} can not be stopped", serviceName, environmentName);
            return false;
        }

        Logger.LogInformation("Service Stopped");
        return true;
    }
}