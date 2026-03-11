using LibAppInstallWork.Models;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork;

public static class ProxySettingsCreator
{
    public static ProxySettingsBase? Create(int serverSidePort, string? apiVersionId, string projectName,
        ServerInfoModel serverInfo)
    {
        if (serverSidePort > 0 && !string.IsNullOrWhiteSpace(apiVersionId))
        {
            return new ProxySettings(serverSidePort, apiVersionId);
        }

        if (serverSidePort == 0)
        {
            if (!string.IsNullOrWhiteSpace(apiVersionId))
            {
                StShared.WriteWarningLine(
                    $"ServerSidePort is 0 and Project ApiVersionId is specified for project {projectName} and server {serverInfo.GetItemKey()}. ApiVersionId wil be ignored.",
                    true);
            }

            return new ProxySettingsBase();
        }

        StShared.WriteErrorLine(
            $"ServerSidePort is {serverSidePort} and Project ApiVersionId does not specified for project {projectName} and server {serverInfo.GetItemKey()}",
            true);
        return null;
    }
}
