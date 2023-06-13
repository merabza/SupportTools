using CliParameters;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramRemover : ToolCommand
{
    private const string ActionName = "Remove App";
    private const string ActionDescription = "Remove App";
    private readonly ServiceStartStopParameters _parameters;

    public ProgramRemover(ILogger logger, bool useConsole, ServiceStartStopParameters parameters,
        IParametersManager parametersManager) : base(logger, useConsole, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        if (!string.IsNullOrWhiteSpace(_parameters.ProjectName))
            return true;

        Logger.LogError("Project Name not specified");
        return false;
    }

    protected override bool RunAction()
    {
        //კლიენტის შექმნა
        var agentClient =
            AgentClientsFabricExt.CreateAgentClient(Logger, _parameters.WebAgentForInstall, _parameters.InstallFolder);
        if (agentClient is null)
        {
            Logger.LogError($"agentClient does not created, Project {_parameters.ProjectName} can not removed");
            return false;
        }

        //Web-აგენტის საშუალებით წაშლის პროცესის გაშვება.
        if (agentClient.RemoveProjectAndService(_parameters.ProjectName, _parameters.ServiceName))
            return true;

        Logger.LogError($"Project {_parameters.ProjectName} can not removed");
        return false;
    }
}