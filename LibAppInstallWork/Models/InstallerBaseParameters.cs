﻿using System.Threading;
using ApiClientsManagement;
using Installer.Domain;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class InstallerBaseParameters
{
    private InstallerBaseParameters(ApiClientSettingsDomain? webAgentForInstall,
        LocalInstallerSettingsDomain? localInstallerSettings)
    {
        WebAgentForInstall = webAgentForInstall;
        LocalInstallerSettings = localInstallerSettings;
    }

    public ApiClientSettingsDomain? WebAgentForInstall { get; }
    public LocalInstallerSettingsDomain? LocalInstallerSettings { get; }

    public static InstallerBaseParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
        ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
        {
            StShared.WriteErrorLine("Server name is not specified", true);
            return null;
        }

        var server = supportToolsParameters.GetServerDataRequired(serverInfo.ServerName);
        ApiClientSettingsDomain? webAgentForInstall = null;
        LocalInstallerSettingsDomain? localInstallerSettingsDomain = null;
        if (!server.IsLocal)
        {
            var webAgentNameForInstall =
                project.UseAlternativeWebAgent ? server.WebAgentInstallerName : server.WebAgentName;
            if (string.IsNullOrWhiteSpace(webAgentNameForInstall))
            {
                StShared.WriteErrorLine(
                    $"webAgentNameForCheck does not specified for Project {projectName} and server {serverInfo.GetItemKey()}",
                    true);
                return null;
            }

            webAgentForInstall = supportToolsParameters.GetApiClientSettingsRequired(webAgentNameForInstall);
        }
        else
        {
            localInstallerSettingsDomain = LocalInstallerSettingsDomain.Create(null, true,
                supportToolsParameters.LocalInstallerSettings, null, null, CancellationToken.None).Result;

            if (localInstallerSettingsDomain is not null)
                return new InstallerBaseParameters(webAgentForInstall, localInstallerSettingsDomain);

            StShared.WriteErrorLine("LocalInstallerSettingsDomain does not Created", true);
            return null;
        }

        return new InstallerBaseParameters(webAgentForInstall, localInstallerSettingsDomain);
    }
}