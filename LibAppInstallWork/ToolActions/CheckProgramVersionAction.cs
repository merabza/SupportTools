using System;
using System.Threading;
using System.Threading.Tasks;
using ApiClientsManagement;
using Installer.AgentClients;
using LibAppInstallWork.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class CheckProgramVersionAction : ToolAction
{
    private readonly string? _installingProgramVersion;
    private readonly int _maxTryCount;
    private readonly ProxySettingsBase _proxySettings;
    private readonly ILogger _logger;
    private readonly ApiClientSettingsDomain _webAgentForCheck;

    public CheckProgramVersionAction(ILogger logger, ApiClientSettingsDomain webAgentForCheck,
        ProxySettingsBase proxySettings, string? installingProgramVersion, int maxTryCount = 10) : base(logger,
        "Check Program Version", null, null)
    {
        _logger = logger;
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
                    // ReSharper disable once using
                    // ReSharper disable once DisposableConstructor
                    await using var proxyApiClient = new ProjectsProxyApiClient(_logger, _webAgentForCheck.Server,
                        _webAgentForCheck.ApiKey, _webAgentForCheck.WithMessaging);
                    var getVersionByProxyResult = await proxyApiClient.GetVersionByProxy(proxySettings.ServerSidePort,
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
                    // ReSharper disable once using
                    // ReSharper disable once DisposableConstructor
                    await using var testApiClient = new TestApiClient(_logger, _webAgentForCheck.Server);
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