//Created by ProjectMainClassCreator at 5/10/2021 16:03:33

using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolCommands;

public sealed class ServiceStarter : ToolCommand
{
    private const string ActionName = "Starting Service";
    private const string ActionDescription = "Starting Service";
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceStartStopParameters _parameters;

    public ServiceStarter(ILogger logger, IHttpClientFactory httpClientFactory, ServiceStartStopParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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

        _logger.LogInformation("Try to start service {projectName}/{environmentName}...", projectName, environmentName);

        if (string.IsNullOrWhiteSpace(projectName))
        {
            _logger.LogError("Service Name not specified");
            return false;
        }

        //კლიენტის შექმნა
        var projectManager = ProjectsManagersFabric.CreateProjectsManager(_logger, _httpClientFactory,
            _parameters.WebAgentForInstall, _parameters.InstallFolder);

        if (projectManager is null)
        {
            _logger.LogError("agentClient does not created. Service {projectName}/{environmentName} can not started",
                projectName, environmentName);
            return false;
        }

        //Web-აგენტის საშუალებით პროცესის გაშვების მცდელობა.
        var startServiceResult = await projectManager.StartService(_parameters.ProjectName, _parameters.EnvironmentName,
            CancellationToken.None);
        if (startServiceResult.IsSome)
        {
            _logger.LogError("Service {projectName}/{environmentName} can not started", projectName, environmentName);
            return false;
        }

        _logger.LogInformation("Service Started");

        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction = new(_logger, _httpClientFactory,
            _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!await checkParametersVersionAction.Run(cancellationToken))
            _logger.LogError("Service {projectName}/{environmentName} parameters file check failed", projectName,
                environmentName);


        //შევამოწმოთ გაშვებული პროგრამის ვერსია
        CheckProgramVersionAction checkProgramVersionAction = new(_logger, _httpClientFactory,
            _parameters.WebAgentForCheck, _parameters.ProxySettings, null, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
            _logger.LogError("Service {projectName}/{environmentName} version check failed", projectName,
                environmentName);

        return true;
    }
}