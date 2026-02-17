//Created by ProjectParametersClassCreator at 5/10/2021 16:03:33

using System;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;
using ToolsManagement.ApiClientsManagement;

namespace LibAppInstallWork.Models;

public sealed class ServiceStartStopParameters : IParameters
{
    private ServiceStartStopParameters(string projectName, string environmentName,
        ApiClientSettingsDomain? webAgentForInstall, string? installFolder, ProxySettingsBase proxySettings,
        ApiClientSettingsDomain webAgentForCheck)
    {
        ProjectName = projectName;
        EnvironmentName = environmentName;
        WebAgentForInstall = webAgentForInstall;
        InstallFolder = installFolder;
        ProxySettings = proxySettings;
        WebAgentForCheck = webAgentForCheck;
    }

    public string ProjectName { get; }
    public string EnvironmentName { get; }

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
            {
                return null;
            }

            ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

            if (checkService && !project.IsService)
            {
                StShared.WriteErrorLine($"Project {projectName} is not service", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(serverInfo.ServerName))
            {
                StShared.WriteErrorLine("Server name is not specified", true);
                return null;
            }

            if (string.IsNullOrWhiteSpace(serverInfo.EnvironmentName))
            {
                StShared.WriteErrorLine("Environment name is not specified", true);
                return null;
            }

            ServerDataModel server = supportToolsParameters.GetServerDataRequired(serverInfo.ServerName);

            ApiClientSettingsDomain? webAgentForInstall = null;
            string? installFolder = null;
            if (!server.IsLocal)
            {
                string? webAgentNameForInstall =
                    project.UseAlternativeWebAgent ? server.WebAgentInstallerName : server.WebAgentName;
                if (string.IsNullOrWhiteSpace(webAgentNameForInstall))
                {
                    StShared.WriteErrorLine(
                        $"webAgentNameForCheck does not specified for Project {projectName} and server {serverInfo.ServerName}",
                        true);
                    return null;
                }

                webAgentForInstall = supportToolsParameters.GetApiClientSettingsRequired(webAgentNameForInstall);
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

            ProxySettingsBase? proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort,
                serverInfo.ApiVersionId, projectName, serverInfo);

            return proxySettings is null
                ? null
                : new ServiceStartStopParameters(projectName, serverInfo.EnvironmentName, webAgentForInstall,
                    installFolder, proxySettings, checkVersionParameters.WebAgentForCheck);
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}
