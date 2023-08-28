using Installer.AgentClients;
using Installer.Domain;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork;

public static class AgentClientsFabricExt
{
    public static IAgentClient CreateWebAgentClient(ILogger logger,
        ApiClientSettingsDomain programUpdaterWebAgentSettings)
    {
        return new WebAgentClient(logger, programUpdaterWebAgentSettings.Server, programUpdaterWebAgentSettings.ApiKey,
            null, null);
    }

    public static IAgentClientWithFileStorage? CreateAgentClientWithFileStorage(ILogger logger,
        FileStorageData fileStorageForUpload, InstallerBaseParameters installerBaseParameters)
    {
        if (installerBaseParameters.WebAgentForInstall is not null &&
            installerBaseParameters.LocalInstallerSettings is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (installerBaseParameters.WebAgentForInstall is not null)
            return new WebAgentClientWithFileStorage(logger, installerBaseParameters.WebAgentForInstall.Server,
                installerBaseParameters.WebAgentForInstall.ApiKey, null, null);
        if (installerBaseParameters.LocalInstallerSettings is not null)
            return new LocalAgentWithFileStorage(logger, false, fileStorageForUpload,
                installerBaseParameters.LocalInstallerSettings, null, null);
        logger.LogError("Both ApiClient Settings and Installer Settings are not specified");
        return null;
    }

    public static IAgentClient? CreateAgentClient(ILogger logger, ApiClientSettingsDomain? programUpdaterWebAgent,
        string? installFolder)
    {
        if (programUpdaterWebAgent is not null && installFolder is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (programUpdaterWebAgent is not null)
            return new WebAgentClient(logger, programUpdaterWebAgent.Server, programUpdaterWebAgent.ApiKey, null, null);
        if (!string.IsNullOrWhiteSpace(installFolder))
            return new LocalAgent(logger, false, installFolder, null, null);
        logger.LogError("Both ApiClient Settings and install Folder are not specified");
        return null;
    }
}