﻿using Installer.Domain;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class InstallerBaseParameters
{
    public InstallerBaseParameters(ApiClientSettingsDomain? webAgentForInstall,
        LocalInstallerSettingsDomain? localInstallerSettings)
    {
        WebAgentForInstall = webAgentForInstall;
        LocalInstallerSettings = localInstallerSettings;
    }

    public ApiClientSettingsDomain? WebAgentForInstall { get; }
    public LocalInstallerSettingsDomain? LocalInstallerSettings { get; }

    public static InstallerBaseParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
        string serverName)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);
        //ServerInfoModel serverInfo = project.GetServerInfoRequired(serverName);
        var server = supportToolsParameters.GetServerDataRequired(serverName);
        ApiClientSettingsDomain? webAgentForInstall = null;
        LocalInstallerSettingsDomain? localInstallerSettingsDomain = null;
        if (!server.IsLocal)
        {
            var webAgentNameForInstall =
                project.UseAlternativeWebAgent ? server.WebAgentInstallerName : server.WebAgentName;
            if (string.IsNullOrWhiteSpace(webAgentNameForInstall))
            {
                StShared.WriteErrorLine(
                    $"webAgentNameForCheck does not specified for Project {projectName} and server {serverName}",
                    true);
                return null;
            }

            webAgentForInstall = supportToolsParameters.GetWebAgentRequired(webAgentNameForInstall);

            //if (string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
            //{
            //    StShared.WriteErrorLine(
            //        $"ApiVersionId does not specified for Project {projectName} and server {serverName}", true);
            //    return null;

            //}
        }
        else
        {
            localInstallerSettingsDomain =
                LocalInstallerSettingsDomain.Create(null, true, supportToolsParameters.LocalInstallerSettings);

            if (localInstallerSettingsDomain is null)
            {
                StShared.WriteErrorLine("LocalInstallerSettingsDomain does not Created", true);
                return null;
            }
        }

        return new InstallerBaseParameters(webAgentForInstall, localInstallerSettingsDomain);
    }
}