//Created by ProjectMainClassCreator at 5/10/2021 16:04:08

using CliParameters;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStopper : ToolCommand
{
    private const string ActionName = "Stop Service";
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStopper(ILogger logger, bool useConsole, ServiceStartStopParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        "Stop Service")
    {
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        Logger.LogInformation($"Try to stop service {_parameters.ServiceName}...");

        if (string.IsNullOrWhiteSpace(_parameters.ServiceName))
        {
            Logger.LogError("Service Name not specified");
            return false;
        }

        //კლიენტის შექმნა
        var agentClient =
            AgentClientsFabricExt.CreateAgentClient(Logger, _parameters.WebAgentForInstall, _parameters.InstallFolder);

        if (agentClient is null)
        {
            Logger.LogError($"agentClient does not created. Service {_parameters.ServiceName} can not be stopped");
            return false;
        }


        //Web-აგენტის საშუალებით პროცესის გაჩერების მცდელობა.
        if (!agentClient.StopService(_parameters.ServiceName))
        {
            Logger.LogError($"Service {_parameters.ServiceName} can not be stopped");
            return false;
        }

        Logger.LogInformation("Service Stopped");
        return true;
    }
}