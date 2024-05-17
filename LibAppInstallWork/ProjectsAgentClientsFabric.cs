using ApiClientsManagement;
using Installer.AgentClients;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace LibAppInstallWork;

public static class ProjectsAgentClientsFabric
{
    public static IIProjectsApiClientWithFileStorage? CreateProjectsApiClientWithFileStorage(ILogger logger,
        IHttpClientFactory httpClientFactory, FileStorageData fileStorageForUpload,
        InstallerBaseParameters installerBaseParameters)
    {
        if (installerBaseParameters.WebAgentForInstall is not null &&
            installerBaseParameters.LocalInstallerSettings is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (installerBaseParameters.WebAgentForInstall is not null)
            //+
            // ReSharper disable once DisposableConstructor
            return new ProjectsApiClientWithFileStorage(logger, httpClientFactory,
                installerBaseParameters.WebAgentForInstall.Server, installerBaseParameters.WebAgentForInstall.ApiKey,
                installerBaseParameters.WebAgentForInstall.WithMessaging);
        if (installerBaseParameters.LocalInstallerSettings is not null)
            return new ProjectsLocalAgentWithFileStorage(logger, false, fileStorageForUpload,
                installerBaseParameters.LocalInstallerSettings, null, null);
        logger.LogError("Both ApiClient Settings and Installer Settings are not specified");
        return null;
    }

    public static IProjectsApiClient? CreateProjectsApiClient(ILogger logger, IHttpClientFactory httpClientFactory,
        ApiClientSettingsDomain? programUpdaterWebAgent, string? installFolder)
    {
        if (programUpdaterWebAgent is not null && installFolder is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (programUpdaterWebAgent is not null)
            //+
            // ReSharper disable once DisposableConstructor
            return new ProjectsApiClient(logger, httpClientFactory, programUpdaterWebAgent.Server,
                programUpdaterWebAgent.ApiKey, programUpdaterWebAgent.WithMessaging);
        if (!string.IsNullOrWhiteSpace(installFolder))
            return new ProjectsLocalAgent(logger, false, installFolder, null, null);
        logger.LogError("Both ApiClient Settings and install Folder are not specified");
        return null;
    }
}