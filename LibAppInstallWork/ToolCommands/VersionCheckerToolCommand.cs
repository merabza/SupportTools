//Created by ProjectMainClassCreator at 5/11/2021 08:52:10

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliParameters;
using LibAppInstallWork.Models;
using LibAppInstallWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

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
        string projectName = CheckVersionParameters.ProjectName;
        //შევამოწმოთ გაშვებული პროგრამის პარამეტრების ვერსია
        var checkParametersVersionAction = new CheckParametersVersionAction(_logger, _httpClientFactory,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, 1, UseConsole);
        if (!await checkParametersVersionAction.Run(cancellationToken))
        {
            _logger.LogError("project {ProjectName} parameters file check failed", projectName);
        }
        //return false;

        //შევამოწმოთ გაშვებული პროგრამის ვერსია 
        var checkProgramVersionAction = new CheckProgramVersionAction(_logger, _httpClientFactory,
            CheckVersionParameters.WebAgentForCheck, CheckVersionParameters.ProxySettings, null, UseConsole, 1);
        if (!await checkProgramVersionAction.Run(cancellationToken))
        {
            _logger.LogError("project {ProjectName} version check failed", projectName);
        }
        //return false;

        return true;
    }
}
