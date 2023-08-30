using Installer.AgentClients;
using Installer.Domain;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;

namespace LibAppInstallWork;

public static class ProjectsAgentClientsFabric
{
    public static IIProjectsApiClientWithFileStorage? CreateProjectsApiClientWithFileStorage(ILogger logger,
        FileStorageData fileStorageForUpload, InstallerBaseParameters installerBaseParameters)
    {
        if (installerBaseParameters.WebAgentForInstall is not null &&
            installerBaseParameters.LocalInstallerSettings is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (installerBaseParameters.WebAgentForInstall is not null)
            return new ProjectsApiClientWithFileStorage(logger, installerBaseParameters.WebAgentForInstall.Server,
                installerBaseParameters.WebAgentForInstall.ApiKey, null, null);
        if (installerBaseParameters.LocalInstallerSettings is not null)
            return new ProjectsLocalAgentWithFileStorage(logger, false, fileStorageForUpload,
                installerBaseParameters.LocalInstallerSettings, null, null);
        logger.LogError("Both ApiClient Settings and Installer Settings are not specified");
        return null;
    }

    public static IProjectsApiClient? CreateProjectsApiClient(ILogger logger,
        ApiClientSettingsDomain? programUpdaterWebAgent,
        string? installFolder)
    {
        if (programUpdaterWebAgent is not null && installFolder is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (programUpdaterWebAgent is not null)
            return new ProjectsApiClient(logger, programUpdaterWebAgent.Server, programUpdaterWebAgent.ApiKey, null,
                null);
        if (!string.IsNullOrWhiteSpace(installFolder))
            return new ProjectsLocalAgent(logger, false, installFolder, null, null);
        logger.LogError("Both ApiClient Settings and install Folder are not specified");
        return null;
    }
}