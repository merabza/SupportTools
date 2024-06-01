using ApiClientsManagement;
using LibAppInstallWork.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SystemToolsShared;
using TestApiContracts;
using WebAgentProjectsApiContracts;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class CheckProgramVersionAction : ToolAction
{
    private readonly string? _installingProgramVersion;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly int _maxTryCount;
    private readonly ProxySettingsBase _proxySettings;
    private readonly ApiClientSettingsDomain _webAgentForCheck;

    public CheckProgramVersionAction(ILogger logger, IHttpClientFactory httpClientFactory,
        ApiClientSettingsDomain webAgentForCheck, ProxySettingsBase proxySettings, string? installingProgramVersion,
        int maxTryCount = 10) : base(logger, "Check Program Version", null, null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _webAgentForCheck = webAgentForCheck;
        _proxySettings = proxySettings;
        _installingProgramVersion = installingProgramVersion;
        _maxTryCount = maxTryCount;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var getVersionSuccess = false;
        var version = "";
        var tryCount = 0;
        while (!getVersionSuccess && tryCount < _maxTryCount)
        {
            if (tryCount > 0)
            {
                _logger.LogInformation("waiting for 3 second...");
                Thread.Sleep(3000);
            }

            tryCount++;
            try
            {
                _logger.LogInformation("Try to get Version {tryCount}...", tryCount);

                if (_proxySettings is ProxySettings proxySettings)
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var projectsApiClient = new ProjectsApiClient(_logger, _httpClientFactory, _webAgentForCheck.Server,
                        _webAgentForCheck.ApiKey);
                    var getVersionByProxyResult =
                        await projectsApiClient.GetVersionByProxy(proxySettings.ServerSidePort,
                            proxySettings.ApiVersionId, cancellationToken);
                    if (getVersionByProxyResult.IsT1)
                    {
                        Err.PrintErrorsOnConsole(getVersionByProxyResult.AsT1);
                        break;
                    }

                    version = getVersionByProxyResult.AsT0;
                }
                else
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var testApiClient = new TestApiClient(_logger, _httpClientFactory, _webAgentForCheck.Server);
                    var getVersionResult = await testApiClient.GetVersion(cancellationToken);
                    if (getVersionResult.IsT1)
                    {
                        Err.PrintErrorsOnConsole(getVersionResult.AsT1);
                        break;
                    }

                    version = getVersionResult.AsT0;
                }

                if (_installingProgramVersion == null)
                {
                    _logger.LogInformation("Project is running on version {version}", version);
                    return true;
                }


                if (_installingProgramVersion != version)
                {
                    _logger.LogWarning("Current version is {version}, but must be {_installingProgramVersion}", version,
                        _installingProgramVersion);
                    getVersionSuccess = false;
                    break;
                }

                //აქ თუ მოვედით, ყველაფერი კარგად არის
                getVersionSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "could not get version on try {tryCount}", tryCount);
            }
        }

        if (!getVersionSuccess)
        {
            _logger.LogError("could not get version");
            return false;
        }

        if (_installingProgramVersion != version)
        {
            _logger.LogError("Current version is {version}, but must be {_installingProgramVersion}", version,
                _installingProgramVersion);
            return false;
        }

        _logger.LogInformation("Project now is running on version {version}", version);

        return true;
    }
}