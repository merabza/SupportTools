using System.Net.Http;
using ApiClientsManagement;
using Installer.ProjectManagers;
using LibAppInstallWork.Models;
using LibFileParameters.Models;
using Microsoft.Extensions.Logging;
using WebAgentProjectsApiContracts;

namespace LibAppInstallWork;

public static class ProjectsManagersFabric
{
    public static IIProjectsManagerWithFileStorage? CreateProjectsManagerWithFileStorage(ILogger logger,
        IHttpClientFactory httpClientFactory, FileStorageData fileStorageForUpload,
        InstallerBaseParameters installerBaseParameters, bool useConsole)
    {
        if (installerBaseParameters.WebAgentForInstall is not null &&
            installerBaseParameters.LocalInstallerSettings is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (installerBaseParameters.WebAgentForInstall is not null)
        {
            var projectsApiClient = new ProjectsApiClient(logger, httpClientFactory,
                installerBaseParameters.WebAgentForInstall.Server, installerBaseParameters.WebAgentForInstall.ApiKey,
                useConsole);
            return new ProjectsManagerRemoteWithFileStorage(projectsApiClient);
        }

        if (installerBaseParameters.LocalInstallerSettings is not null)
            return new ProjectsManagerLocalWithFileStorage(logger, false, fileStorageForUpload,
                installerBaseParameters.LocalInstallerSettings, null, null);
        logger.LogError("Both ApiClient Settings and Installer Settings are not specified");
        return null;
    }

    public static IProjectsManager? CreateProjectsManager(ILogger logger, IHttpClientFactory httpClientFactory,
        ApiClientSettingsDomain? programUpdaterWebAgent, string? installFolder, bool useConsole)
    {
        if (programUpdaterWebAgent is not null && installFolder is not null)
        {
            logger.LogError("Both ApiClient Settings and Installer Settings are specified. must be only one");
            return null;
        }

        if (programUpdaterWebAgent is not null)
        {
            var projectsApiClient = new ProjectsApiClient(logger, httpClientFactory, programUpdaterWebAgent.Server,
                programUpdaterWebAgent.ApiKey, useConsole);
            return new ProjectsManagerRemote(projectsApiClient);
        }

        if (!string.IsNullOrWhiteSpace(installFolder))
            return new ProjectsManagerLocal(logger, false, installFolder, null, null);
        logger.LogError("Both ApiClient Settings and install Folder are not specified");
        return null;
    }
}