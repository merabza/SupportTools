using System;
using ApiClientsManagement;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ProgramRemoverParameters : IParameters
{
    private ProgramRemoverParameters(string projectName, string environmentName, bool isService,
        ApiClientSettingsDomain? webAgentForInstall, string? installFolder
        //, ProxySettingsBase proxySettings,
        //ApiClientSettingsDomain webAgentForCheck
    )
    {
        ProjectName = projectName;
        EnvironmentName = environmentName;
        IsService = isService;
        WebAgentForInstall = webAgentForInstall;
        InstallFolder = installFolder;
        //ProxySettings = proxySettings;
        //WebAgentForCheck = webAgentForCheck;
    }

    public string ProjectName { get; }
    public string EnvironmentName { get; }
    public bool IsService { get; }

    public ApiClientSettingsDomain? WebAgentForInstall { get; }

    public string? InstallFolder { get; }
    //public ProxySettingsBase ProxySettings { get; }

    //public ApiClientSettingsDomain WebAgentForCheck { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ProgramRemoverParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
        ServerInfoModel serverInfo)
    {
        try
        {
            var checkVersionParameters = CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
            if (checkVersionParameters is null)
                return null;

            var project = supportToolsParameters.GetProjectRequired(projectName);

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

            return proxySettings is null
                ? null
                : new ProgramRemoverParameters(projectName, serverInfo.EnvironmentName, project.IsService,
                    webAgentForInstall, installFolder); //, proxySettings, checkVersionParameters.WebAgentForCheck);
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}