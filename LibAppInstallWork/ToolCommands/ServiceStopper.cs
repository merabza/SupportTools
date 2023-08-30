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

    public ServiceStopper(ILogger logger, ServiceStartStopParameters parameters, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, "Stop Service")
    {
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var serviceName = _parameters.ServiceName;
        Logger.LogInformation("Try to stop service {serviceName}...", serviceName);

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            Logger.LogError("Service Name not specified");
            return false;
        }

        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabricExt.CreateProjectsApiClient(Logger, _parameters.WebAgentForInstall, _parameters.InstallFolder);

        if (agentClient is null)
        {
            Logger.LogError("agentClient does not created. Service {serviceName} can not be stopped", serviceName);
            return false;
        }


        //Web-აგენტის საშუალებით პროცესის გაჩერების მცდელობა.
        if (!agentClient.StopService(serviceName).Result)
        {
            Logger.LogError("Service {serviceName} can not be stopped", serviceName);
            return false;
        }

        Logger.LogInformation("Service Stopped");
        return true;
    }
}