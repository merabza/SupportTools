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

    protected override bool RunAction()
    {
        var serviceName = _parameters.ServiceName;
        Logger.LogInformation("Try to start service {serviceName}...", serviceName);

        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabricExt.CreateProjectsApiClient(Logger, _parameters.WebAgentForInstall, _parameters.InstallFolder);

        if (agentClient is null)
        {
            Logger.LogError("agentClient does not created. Service {serviceName} can not started", serviceName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაშვების მცდელობა.
        if (!agentClient.StartService(_parameters.ServiceName).Result)
        {
            Logger.LogError("Service {serviceName} can not started", serviceName);
            return false;
        }

        Logger.LogInformation("Service Started");

        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction =
            new(Logger, _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!checkParametersVersionAction.Run())
            Logger.LogError("Service {serviceName} parameters file check failed", serviceName);


        //შევამოწმოთ გაშვებული პროგრამის ვერსია ServiceStartParameters.ServiceName, 
        CheckProgramVersionAction checkProgramVersionAction =
            new(Logger, _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!checkProgramVersionAction.Run())
            Logger.LogError("Service {serviceName} version check failed", serviceName);

        return true;
    }
}