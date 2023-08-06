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

    public CheckParametersVersionAction(ILogger logger, bool useConsole, ApiClientSettingsDomain webAgentForCheck,
        ProxySettingsBase proxySettings, string? appSettingsVersion, int maxTryCount = 10) : base(logger, useConsole,
        "Check Parameters Version")
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
        //კლიენტის შექმნა ვერსიის შესამოწმებლად
        var agentClientForVersion = AgentClientsFabricExt.CreateWebAgentClient(Logger, _webAgentForCheck);

        if (agentClientForVersion is not WebAgentClient webAgentClientForVersion)
            return false;

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
                Logger.LogInformation($"Try to get parameters Version {tryCount}...");

                version = _proxySettings is ProxySettings proxySettings
                    ? webAgentClientForVersion
                        .GetAppSettingsVersionByProxy(proxySettings.ServerSidePort, proxySettings.ApiVersionId).Result
                    : webAgentClientForVersion.GetAppSettingsVersion().Result ?? "";

                getVersionSuccess = true;

                if (_appSettingsVersion == null)
                {
                    Logger.LogInformation($"{ProjectName} is running on parameters version {version}");
                    return true;
                }

                if ("\"" + _appSettingsVersion + "\"" != version)
                {
                    Logger.LogWarning(
                        $"Current Parameters version is {version}, but must be {_appSettingsVersion}");
                    getVersionSuccess = false;
                }
            }
            catch
            {
                Logger.LogWarning($"could not get Parameters version for project {ProjectName} on try {tryCount}");
            }
        }

        if (!getVersionSuccess)
        {
            Logger.LogError($"could not get parameters version for project {ProjectName}");
            return false;
        }

        if ("\"" + _appSettingsVersion + "\"" != version)
        {
            Logger.LogError($"Current parameters version is {version}, but must be {_appSettingsVersion}");
            return false;
        }

        Logger.LogInformation($"{ProjectName} now is running on parameters version {version}");
        return true;
    }
}