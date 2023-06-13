//Created by ProjectMainClassCreator at 5/10/2021 16:03:33

using CliParameters;
using LibAppInstallWork.Actions;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStarter : ToolCommand
{
    private const string ActionName = "Starting Service";
    private const string ActionDescription = "Starting Service";
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStarter(ILogger logger, bool useConsole, ServiceStartStopParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        if (string.IsNullOrWhiteSpace(_parameters.ServiceName))
        {
            Logger.LogError("Service Name not specified");
            return false;
        }

        return true;
    }

    protected override bool RunAction()
    {
        Logger.LogInformation($"Try to start service {_parameters.ServiceName}...");

        //კლიენტის შექმნა
        var agentClient =
            AgentClientsFabricExt.CreateAgentClient(Logger, _parameters.WebAgentForInstall, _parameters.InstallFolder);

        if (agentClient is null)
        {
            Logger.LogError($"agentClient does not created. Service {_parameters.ServiceName} can not started");
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაშვების მცდელობა.
        if (!agentClient.StartService(_parameters.ServiceName))
        {
            Logger.LogError($"Service {_parameters.ServiceName} can not started");
            return false;
        }

        Logger.LogInformation("Service Started");

        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction = new(Logger, UseConsole,
            _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!checkParametersVersionAction.Run())
            Logger.LogError($"Service {_parameters.ServiceName} parameters file check failed");


        //შევამოწმოთ გაშვებული პროგრამის ვერსია ServiceStartParameters.ServiceName, 
        CheckProgramVersionAction checkProgramVersionAction = new(Logger, UseConsole, _parameters.WebAgentForCheck,
            _parameters.ProxySettings, null, 1);
        if (!checkProgramVersionAction.Run())
            Logger.LogError($"Service {_parameters.ServiceName} version check failed");

        return true;
    }
}