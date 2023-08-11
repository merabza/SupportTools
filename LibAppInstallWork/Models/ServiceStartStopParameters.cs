//Created by ProjectParametersClassCreator at 5/10/2021 16:03:33

using System;
using Installer.Domain;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ServiceStartStopParameters : IParameters
{
    private ServiceStartStopParameters(string projectName, string serviceName,
        ApiClientSettingsDomain? webAgentForInstall, string? installFolder, ProxySettingsBase proxySettings,
        ApiClientSettingsDomain webAgentForCheck)
    {
        ProjectName = projectName;
        ServiceName = serviceName;
        WebAgentForInstall = webAgentForInstall;
        InstallFolder = installFolder;
        ProxySettings = proxySettings;
        WebAgentForCheck = webAgentForCheck;
    }

    public string ProjectName { get; }
    public string ServiceName { get; }
    public ApiClientSettingsDomain? WebAgentForInstall { get; }
    public string? InstallFolder { get; }
    public ProxySettingsBase ProxySettings { get; }

    public ApiClientSettingsDomain WebAgentForCheck { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ServiceStartStopParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
        ServerInfoModel serverInfo, bool checkService = true)
    {
        try
        {
            var checkVersionParameters = CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
            if (checkVersionParameters is null)
                return null;

            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (checkService && !project.IsService)
            {
                StShared.WriteErrorLine($"Project {projectName} is not service", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(project.ServiceName))
            {
                StShared.WriteErrorLine(
                    $"Service Name does not specified for Project {projectName} and server {serverInfo.ServerName}",
                    true);
                return null;
            }

            var server = supportToolsParameters.GetServerDataRequired(serverInfo.ServerName);

            ApiClientSettingsDomain? webAgentForInstall = null;
            string? installFolder = null;
            if (!server.IsLocal)
            {
                var webAgentNameForInstall =
                    project.UseAlternativeWebAgent ? server.WebAgentInstallerName : server.WebAgentName;
                if (string.IsNullOrWhiteSpace(webAgentNameForInstall))
                {
                    StShared.WriteErrorLine(
                        $"webAgentNameForCheck does not specified for Project {projectName} and server {serverInfo.ServerName}",
                        true);
                    return null;
                }

                webAgentForInstall = supportToolsParameters.GetWebAgentRequired(webAgentNameForInstall);
            }
            else
            {
                installFolder = supportToolsParameters.LocalInstallerSettings?.InstallFolder;
                if (string.IsNullOrWhiteSpace(installFolder))
                {
                    StShared.WriteErrorLine(
                        $"Server {serverInfo.ServerName} is local, but installFolder does not specified is Parameters",
                        true);
                    return null;
                }
            }

            var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId,
                projectName, serverInfo);

            if (proxySettings is null)
                return null;

            return new ServiceStartStopParameters(projectName, project.ServiceName, webAgentForInstall, installFolder,
                proxySettings, checkVersionParameters.WebAgentForCheck);
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}