﻿using System;
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

    public CheckProgramVersionAction(ILogger logger, bool useConsole, ApiClientSettingsDomain webAgentForCheck,
        ProxySettingsBase proxySettings, string? installingProgramVersion, int maxTryCount = 10) : base(logger,
        useConsole, "Check Program Version")
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
                Logger.LogInformation($"Try to get Version {tryCount}...");

                version = _proxySettings is ProxySettings proxySettings
                    ? webAgentClientForVersion
                        .GetVersionByProxy(proxySettings.ServerSidePort, proxySettings.ApiVersionId).Result
                    : webAgentClientForVersion.GetVersion().Result ?? "";

                if (_installingProgramVersion == null)
                {
                    Logger.LogInformation($"Project is running on version {version}");
                    return true;
                }


                if (_installingProgramVersion != version)
                {
                    Logger.LogWarning($"Current version is {version}, but must be {_installingProgramVersion}");
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

        if (_installingProgramVersion != version)
        {
            Logger.LogError($"Current version is {version}, but must be {_installingProgramVersion}");
            return false;
        }

        Logger.LogInformation($"Project now is running on version {version}");

        return true;
    }
}