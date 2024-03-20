//Created by ProjectParametersClassCreator at 5/11/2021 08:52:10

using System;
using ApiClientsManagement;
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
        string projectName, ServerInfoModel serverInfo, bool checkService = true)
    {
        try
        {
            var project = supportToolsParameters.GetProjectRequired(projectName);

            if (checkService && !project.IsService)
            {
                StShared.WriteErrorLine($"Project {projectName} is not service", true);
                return null;
            }

            var webAgentNameForCheck = serverInfo.WebAgentNameForCheck;
            if (string.IsNullOrWhiteSpace(webAgentNameForCheck))
            {
                StShared.WriteErrorLine(
                    $"webAgentNameForCheck does not specified for Project {projectName} and server {serverInfo.GetItemKey()}",
                    true);
                return null;
            }

            var webAgentForCheck = supportToolsParameters.GetWebAgentRequired(webAgentNameForCheck);

            var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId,
                projectName, serverInfo);

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