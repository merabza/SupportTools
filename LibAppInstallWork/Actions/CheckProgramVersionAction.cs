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

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
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
                    var testApiClient = new TestApiClient(Logger, _webAgentForCheck.Server);
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
                    Logger.LogInformation("Project is running on version {version}", version);
                    return true;
                }


                if (_installingProgramVersion != version)
                {
                    Logger.LogWarning("Current version is {version}, but must be {_installingProgramVersion}", version,
                        _installingProgramVersion);
                    getVersionSuccess = false;
                    break;
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

        if (_installingProgramVersion != version)
        {
            Logger.LogError("Current version is {version}, but must be {_installingProgramVersion}", version,
                _installingProgramVersion);
            return false;
        }

        Logger.LogInformation("Project now is running on version {version}", version);

        return true;
    }
}