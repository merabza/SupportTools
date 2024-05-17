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

public sealed class VersionChecker : ToolCommand
{
    private const string ActionName = "Check Version";
    private const string ActionDescription = "Check Version";
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VersionChecker(ILogger logger, IHttpClientFactory httpClientFactory, CheckVersionParameters parameters,
        IParametersManager parametersManager) : base(logger, ActionName, parameters, parametersManager,
        ActionDescription)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private CheckVersionParameters CheckVersionParameters => (CheckVersionParameters)Par;

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var projectName = CheckVersionParameters.ProjectName;
        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        CheckParametersVersionAction checkParametersVersionAction = new(_logger, _httpClientFactory,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, 1);
        if (!await checkParametersVersionAction.Run(cancellationToken))
            _logger.LogError("project {projectName} parameters file check failed", projectName);
        //return false;


        //შევამოწმოთ გაშვებული პროგრამის ვერსია 
        CheckProgramVersionAction checkProgramVersionAction = new(_logger, _httpClientFactory,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
            _logger.LogError("project {projectName} version check failed", projectName);
        //return false;

        return true;
    }
}