//Created by ProjectMainClassCreator at 5/11/2021 08:52:10

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.ToolCommands;

public sealed class VersionCheckerToolCommand : ToolCommand
{
    private const string ActionName = "Check Version";
    private const string ActionDescription = "Check Version";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VersionCheckerToolCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        CheckVersionParameters parameters, IParametersManager parametersManager, bool useConsole) : base(logger,
        ActionName, parameters, parametersManager, ActionDescription, useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private CheckVersionParameters CheckVersionParameters => (CheckVersionParameters)Par;

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        var projectName = CheckVersionParameters.ProjectName;
        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction = new(_logger, _httpClientFactory,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, 1, UseConsole);
        if (!await checkParametersVersionAction.Run(cancellationToken))
            _logger.LogError("project {projectName} parameters file check failed", projectName);
        //return false;

        //შევამოწმოთ გაშვებული პროგრამის ვერსია 
        CheckProgramVersionAction checkProgramVersionAction = new(_logger, _httpClientFactory,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, UseConsole, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
            _logger.LogError("project {projectName} version check failed", projectName);
        //return false;

        return true;
    }
}