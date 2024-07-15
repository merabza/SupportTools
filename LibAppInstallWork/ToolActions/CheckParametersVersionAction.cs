using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiClientsManagement;
using LibAppInstallWork.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SystemToolsShared.Errors;
using TestApiContracts;
using WebAgentProjectsApiContracts;

// ReSharper disable ReplaceWithPrimaryConstructorParameter

namespace LibAppInstallWork.ToolActions;

public sealed class CheckParametersVersionAction : ToolAction
{
    private const string ProjectName = "";
    private readonly string? _appSettingsVersion;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly int _maxTryCount;
    private readonly ProxySettingsBase _proxySettings;
    private readonly ApiClientSettingsDomain _webAgentForCheck;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckParametersVersionAction(ILogger logger, IHttpClientFactory httpClientFactory,
        ApiClientSettingsDomain webAgentForCheck, ProxySettingsBase proxySettings, string? appSettingsVersion,
        int maxTryCount = 10, bool useConsole = false) : base(logger, "Check Parameters Version", null, null,
        useConsole)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _appSettingsVersion = appSettingsVersion;
        _maxTryCount = maxTryCount;
        _proxySettings = proxySettings;
        _webAgentForCheck = webAgentForCheck;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        var getVersionSuccess = false;
        var version = string.Empty;
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
                _logger.LogInformation("Try to get parameters Version {tryCount}...", tryCount);

                var errors = new List<Err>();

                if (_proxySettings is ProxySettings proxySettings)
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var projectsApiClient = new ProjectsApiClient(_logger, _httpClientFactory, _webAgentForCheck.Server,
                        _webAgentForCheck.ApiKey, UseConsole);
                    var getAppSettingsVersionByProxyResult =
                        await projectsApiClient.GetAppSettingsVersionByProxy(proxySettings.ServerSidePort,
                            proxySettings.ApiVersionId, cancellationToken);
                    if (getAppSettingsVersionByProxyResult.IsT1)
                        errors.AddRange(getAppSettingsVersionByProxyResult.AsT1);
                    else
                        version = getAppSettingsVersionByProxyResult.AsT0;
                }
                else
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var testApiClient =
                        new TestApiClient(_logger, _httpClientFactory, _webAgentForCheck.Server, UseConsole);
                    var getAppSettingsVersionResult = await testApiClient.GetAppSettingsVersion(cancellationToken);
                    if (getAppSettingsVersionResult.IsT1)
                        errors.AddRange(getAppSettingsVersionResult.AsT1);
                    else
                        version = getAppSettingsVersionResult.AsT0;
                }

                if (errors.Count > 0)
                {
                    Err.PrintErrorsOnConsole(errors);
                }
                else
                {
                    getVersionSuccess = true;

                    if (_appSettingsVersion == null)
                    {
                        _logger.LogInformation("{ProjectName} is running on parameters version {version}", ProjectName,
                            version);
                        return true;
                    }

                    if (_appSettingsVersion != version)
                    {
                        _logger.LogWarning("Current Parameters version is {version}, but must be {_appSettingsVersion}",
                            version, _appSettingsVersion);
                        getVersionSuccess = false;
                    }
                }
            }
            catch
            {
                _logger.LogWarning("could not get Parameters version for project {ProjectName} on try {tryCount}",
                    ProjectName, tryCount);
            }
        }

        if (!getVersionSuccess)
        {
            _logger.LogError("could not get parameters version for project {ProjectName}", ProjectName);
            return false;
        }

        if (_appSettingsVersion != version)
        {
            _logger.LogError("Current parameters version is {version}, but must be {_appSettingsVersion}", version,
                _appSettingsVersion);
            return false;
        }

        _logger.LogInformation("{ProjectName} now is running on parameters version {version}", ProjectName, version);
        return true;
    }
}