using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class ProgramRemoverToolCommand : ToolCommand
{
    private const string ActionName = "Remove App";
    private const string ActionDescription = "Remove App";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ProgramRemoverParameters _parameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProgramRemoverToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ProgramRemoverParameters parameters, IParametersManager parametersManager, bool useConsole) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parameters = parameters;
    }

    protected override bool CheckValidate()
    {
        if (!string.IsNullOrWhiteSpace(_parameters.ProjectName))
            return true;

        _logger.LogError("Project Name not specified");
        return false;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var projectName = _parameters.ProjectName;
        //კლიენტის შექმნა
        var projectManager = ProjectsManagersFactory.CreateProjectsManager(_logger, _httpClientFactory,
            _parameters.WebAgentForInstall, _parameters.InstallFolder, UseConsole);
        if (projectManager is null)
        {
            _logger.LogError("agentClient does not created, Project {projectName} can not removed", projectName);
            return false;
        }

        //Web-აგენტის საშუალებით წაშლის პროცესის გაშვება.
        if (await projectManager.RemoveProjectAndService(projectName, _parameters.EnvironmentName,
                _parameters.IsService, CancellationToken.None))
            return true;

        _logger.LogError("Project {projectName} can not removed", projectName);
        return false;
    }
}