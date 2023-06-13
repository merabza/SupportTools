//Created by ProjectParametersClassCreator at 5/11/2021 08:52:10

using System;
using Installer.Domain;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class CheckVersionParameters : IParameters
{
    private CheckVersionParameters(string projectName, ApiClientSettingsDomain webAgentForCheck,
        ProxySettingsBase proxySettings)
    {
        ProjectName = projectName;
        WebAgentForCheck = webAgentForCheck;
        ProxySettings = proxySettings;
    }

    public string ProjectName { get; }
    public ApiClientSettingsDomain WebAgentForCheck { get; }

    public ProxySettingsBase ProxySettings { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static CheckVersionParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, string serverName, bool checkService = true)
    {
        try
        {
            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (checkService && !project.IsService)
            {
                StShared.WriteErrorLine($"Project {projectName} is not service", true);
                return null;
            }

            var serverInfo = project.GetServerInfoRequired(serverName);

            //if (string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
            //{
            //    StShared.WriteErrorLine(
            //        $"serverInfo.ApiVersionId does not specified for Project {projectName} and server {serverName}",
            //        true);
            //    return null;
            //}

            var webAgentNameForCheck = serverInfo.WebAgentNameForCheck;
            if (string.IsNullOrWhiteSpace(webAgentNameForCheck))
            {
                StShared.WriteErrorLine(
                    $"webAgentNameForCheck does not specified for Project {projectName} and server {serverName}",
                    true);
                return null;
            }

            //if (serverInfo.ServerSidePort > 0 && string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
            //{
            //    StShared.WriteErrorLine(
            //        $"ServerSidePort is {serverInfo.ServerSidePort} and Project ApiVersionId does not specified for project {projectName} and server {serverName}",
            //        true);
            //    return null;
            //}

            //if (serverInfo.ServerSidePort == 0 && !string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
            //    StShared.WriteWarningLine(
            //        $"ServerSidePort is 0 and Project ApiVersionId is specified for project {projectName} and server {serverName}. ApiVersionId wil be ignored.",
            //        true);

            var webAgentForCheck = supportToolsParameters.GetWebAgentRequired(webAgentNameForCheck);

            var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId,
                projectName,
                serverName);

            if (proxySettings is null)
                return null;

            CheckVersionParameters checkVersionParameters = new(projectName, webAgentForCheck, proxySettings);

            return checkVersionParameters;
        }
        catch (Exception e)
        {
            StShared.WriteErrorLine(e.Message, true);
            return null;
        }
    }
}