using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LibAppInstallWork.Models;
using Microsoft.Extensions.Logging;
using OneOf;
using SystemTools.SystemToolsShared.Errors;
using SystemTools.TestApiContracts;
using ToolsManagement.ApiClientsManagement;
using ToolsManagement.LibToolActions;
using WebAgentContracts.WebAgentProjectsApiContracts;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibAppInstallWork.ToolActions;

public sealed class CheckProgramVersionAction : ToolAction
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _installingProgramVersion;
    private readonly ILogger _logger;
    private readonly int _maxTryCount;
    private readonly ProxySettingsBase _proxySettings;
    private readonly bool _useConsole;
    private readonly ApiClientSettingsDomain _webAgentForCheck;

    public CheckProgramVersionAction(ILogger logger, IHttpClientFactory httpClientFactory,
        ApiClientSettingsDomain webAgentForCheck, ProxySettingsBase proxySettings, string? installingProgramVersion,
        bool useConsole, int maxTryCount = 10) : base(logger, "Check Program Version", null, null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _webAgentForCheck = webAgentForCheck;
        _proxySettings = proxySettings;
        _installingProgramVersion = installingProgramVersion;
        _useConsole = useConsole;
        _maxTryCount = maxTryCount;
    }

    protected override async ValueTask<bool> RunAction(CancellationToken cancellationToken = default)
    {
        bool getVersionSuccess = false;
        string? version = string.Empty;
        int tryCount = 0;
        while (!getVersionSuccess && tryCount < _maxTryCount)
        {
            if (tryCount > 0)
            {
                _logger.LogInformation("waiting for 3 second...");
                await Task.Delay(3000, cancellationToken);
            }

            tryCount++;
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Try to get Version {TryCount}...", tryCount);
                }

                if (_proxySettings is ProxySettings proxySettings)
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var projectsApiClient = new ProjectsApiClient(_logger, _httpClientFactory, _webAgentForCheck.Server,
                        _webAgentForCheck.ApiKey, _useConsole);
                    OneOf<string, Err[]> getVersionByProxyResult =
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
                    var testApiClient = new TestApiClient(_logger, _httpClientFactory, _webAgentForCheck.Server,
                        _useConsole);
                    OneOf<string, Err[]> getVersionResult = await testApiClient.GetVersion(cancellationToken);
                    if (getVersionResult.IsT1)
                    {
                        Err.PrintErrorsOnConsole(getVersionResult.AsT1);
                        break;
                    }

                    version = getVersionResult.AsT0;
                }

                if (_installingProgramVersion == null)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Project is running on version {Version}", version);
                    }

                    return true;
                }

                if (_installingProgramVersion != version)
                {
                    _logger.LogWarning("Current version is {Version}, but must be {InstallingProgramVersion}", version,
                        _installingProgramVersion);
                    getVersionSuccess = false;
                    break;
                }

                //აქ თუ მოვედით, ყველაფერი კარგად არის
                getVersionSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "could not get version on try {TryCount}", tryCount);
            }
        }

        if (!getVersionSuccess)
        {
            _logger.LogError("could not get version");
            return false;
        }

        if (_installingProgramVersion != version)
        {
            _logger.LogError("Current version is {Version}, but must be {InstallingProgramVersion}", version,
                _installingProgramVersion);
            return false;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Project now is running on version {Version}", version);
        }

        return true;
    }
}
