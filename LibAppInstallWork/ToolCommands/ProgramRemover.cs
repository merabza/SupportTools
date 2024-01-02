using System.Threading;
using System.Threading.Tasks;
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

    public ProgramRemover(ILogger logger, ServiceStartStopParameters parameters, IParametersManager parametersManager) :
        base(logger, ActionName, parameters, parametersManager, ActionDescription)
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

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var projectName = _parameters.ProjectName;
        //კლიენტის შექმნა
        var agentClient =
            ProjectsAgentClientsFabric.CreateProjectsApiClient(Logger, _parameters.WebAgentForInstall,
                _parameters.InstallFolder);
        if (agentClient is null)
        {
            Logger.LogError("agentClient does not created, Project {projectName} can not removed", projectName);
            return false;
        }

        //Web-აგენტის საშუალებით წაშლის პროცესის გაშვება.
        if (await agentClient.RemoveProjectAndService(projectName, _parameters.ServiceName, _parameters.EnvironmentName,
                CancellationToken.None))
            return true;

        Logger.LogError("Project {projectName} can not removed", projectName);
        return false;
    }
}