using System.Threading;
using Installer.AgentClients;
using Installer.Domain;
using LibAppInstallWork.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class CheckParametersVersionAction : ToolAction
{
    private const string ProjectName = "";

    private readonly string? _appSettingsVersion;
    private readonly int _maxTryCount;

    private readonly ProxySettingsBase _proxySettings;

    private readonly ApiClientSettingsDomain _webAgentForCheck;

    public CheckParametersVersionAction(ILogger logger, ApiClientSettingsDomain webAgentForCheck,
        ProxySettingsBase proxySettings, string? appSettingsVersion, int maxTryCount = 10) : base(logger,
        "Check Parameters Version", null, null)
    {
        _webAgentForCheck = webAgentForCheck;
        _appSettingsVersion = appSettingsVersion;
        _maxTryCount = maxTryCount;
        _proxySettings = proxySettings;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        var getVersionSuccess = false;
        var version = "";
        var tryCount = 0;
        while (!getVersionSuccess && tryCount < _maxTryCount)
        {
            if (tryCount > 0)
            {
                Logger.LogInformation("waiting for 3 second...");
                Thread.Sleep(3000);
            }

            tryCount++;
            try
            {
                Logger.LogInformation("Try to get parameters Version {tryCount}...", tryCount);

                if (_proxySettings is ProxySettings proxySettings)
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var proxyApiClient =
                        new ProjectsProxyApiClient(Logger, _webAgentForCheck.Server, _webAgentForCheck.ApiKey);
                    version = proxyApiClient
                        .GetAppSettingsVersionByProxy(proxySettings.ServerSidePort, proxySettings.ApiVersionId,
                            CancellationToken.None).Result;
                }
                else
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var testApiClient = new TestApiClient(Logger, _webAgentForCheck.Server);
                    version = testApiClient.GetAppSettingsVersion(CancellationToken.None).Result ?? "";
                }

                getVersionSuccess = true;

                if (_appSettingsVersion == null)
                {
                    Logger.LogInformation("{ProjectName} is running on parameters version {version}", ProjectName,
                        version);
                    return true;
                }

                if (_appSettingsVersion != version)
                {
                    Logger.LogWarning("Current Parameters version is {version}, but must be {_appSettingsVersion}",
                        version, _appSettingsVersion);
                    getVersionSuccess = false;
                }
            }
            catch
            {
                Logger.LogWarning("could not get Parameters version for project {ProjectName} on try {tryCount}",
                    ProjectName, tryCount);
            }
        }

        if (!getVersionSuccess)
        {
            Logger.LogError("could not get parameters version for project {ProjectName}", ProjectName);
            return false;
        }

        if (_appSettingsVersion != version)
        {
            Logger.LogError("Current parameters version is {version}, but must be {_appSettingsVersion}", version,
                _appSettingsVersion);
            return false;
        }

        Logger.LogInformation("{ProjectName} now is running on parameters version {version}", ProjectName, version);
        return true;
    }
}