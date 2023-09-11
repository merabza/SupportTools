using System;
using System.Threading;
using Installer.AgentClients;
using Installer.Domain;
using LibAppInstallWork.Models;
using LibToolActions;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork.Actions;

public sealed class CheckProgramVersionAction : ToolAction
{
    private readonly string? _installingProgramVersion;
    private readonly int _maxTryCount;

    private readonly ProxySettingsBase _proxySettings;

    private readonly ApiClientSettingsDomain _webAgentForCheck;

    public CheckProgramVersionAction(ILogger logger, ApiClientSettingsDomain webAgentForCheck,
        ProxySettingsBase proxySettings, string? installingProgramVersion, int maxTryCount = 10) : base(logger,
        "Check Program Version", null, null)
    {
        _webAgentForCheck = webAgentForCheck;
        _proxySettings = proxySettings;
        _installingProgramVersion = installingProgramVersion;
        _maxTryCount = maxTryCount;
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
                Logger.LogInformation("Try to get Version {tryCount}...", tryCount);

                if (_proxySettings is ProxySettings proxySettings)
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var proxyApiClient =
                        new ProjectsProxyApiClient(Logger, _webAgentForCheck.Server, _webAgentForCheck.ApiKey);
                    version = proxyApiClient.GetVersionByProxy(proxySettings.ServerSidePort, proxySettings.ApiVersionId,
                            CancellationToken.None)
                        .Result;
                }
                else
                {
                    //კლიენტის შექმნა ვერსიის შესამოწმებლად
                    var testApiClient = new TestApiClient(Logger, _webAgentForCheck.Server);
                    version = testApiClient.GetVersion(CancellationToken.None).Result ?? "";
                }

                if (_installingProgramVersion == null)
                {
                    Logger.LogInformation("Project is running on version {version}", version);
                    return true;
                }


                if ("\"" + _installingProgramVersion + "\"" != version)
                {
                    Logger.LogWarning("Current version is {version}, but must be {_installingProgramVersion}", version,
                        _installingProgramVersion);
                    getVersionSuccess = false;
                }

                //აქ თუ მოვედით, ყველაფერი კარგად არის
                getVersionSuccess = true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "could not get version on try {tryCount}", tryCount);
                //Logger.LogWarning($"could not get version on try {tryCount}");
            }
        }

        if (!getVersionSuccess)
        {
            Logger.LogError("could not get version");
            return false;
        }

        if ("\"" + _installingProgramVersion + "\"" != version)
        {
            Logger.LogError("Current version is {version}, but must be {_installingProgramVersion}", version,
                _installingProgramVersion);
            return false;
        }

        Logger.LogInformation("Project now is running on version {version}", version);

        return true;
    }
}