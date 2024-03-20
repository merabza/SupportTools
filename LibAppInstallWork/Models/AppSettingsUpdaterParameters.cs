//Created by ProjectParametersClassCreator at 1/11/2021 20:04:36

using System.Threading;
using ApiClientsManagement;
using Installer.Domain;
using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class AppSettingsUpdaterParameters : IParameters
{
    private AppSettingsUpdaterParameters(string projectName, string environmentName, string? serviceName,
        InstallerBaseParameters installerBaseParameters, ProxySettingsBase proxySettings,
        AppSettingsEncoderParameters appSettingsEncoderParameters, ApiClientSettingsDomain webAgentForCheck,
        string parametersFileDateMask, string parametersFileExtension, FileStorageData fileStorageForUpload)
    {
        ProjectName = projectName;
        EnvironmentName = environmentName;
        ServiceName = serviceName;
        InstallerBaseParameters = installerBaseParameters;
        ProxySettings = proxySettings;
        AppSettingsEncoderParameters = appSettingsEncoderParameters;
        WebAgentForCheck = webAgentForCheck;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
        FileStorageForUpload = fileStorageForUpload;
    }

    public string ProjectName { get; }
    public string EnvironmentName { get; }
    public string? ServiceName { get; }
    public InstallerBaseParameters InstallerBaseParameters { get; }
    public ProxySettingsBase ProxySettings { get; }
    public ApiClientSettingsDomain WebAgentForCheck { get; }
    public AppSettingsEncoderParameters AppSettingsEncoderParameters { get; }
    public string ParametersFileDateMask { get; }
    public string ParametersFileExtension { get; }
    public FileStorageData FileStorageForUpload { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }


    public static AppSettingsUpdaterParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        var checkVersionParameters = CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (checkVersionParameters is null)
            return null;

        var serviceStartStopParameters =
            ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceStartStopParameters is null)
            return null;

        var project = supportToolsParameters.GetProjectRequired(projectName);
        var environmentName = serverInfo.EnvironmentName;
        var serverName = serverInfo.ServerName;


        if (string.IsNullOrWhiteSpace(serverName))
        {
            StShared.WriteErrorLine("Server name is not specified", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            StShared.WriteErrorLine("Environment Name is not specified", true);
            return null;
        }


        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        var appSettingsEncoderParameters =
            AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (appSettingsEncoderParameters == null)
            return null;

        var parametersFileDateMask =
            project.ParametersFileDateMask ?? supportToolsParameters.ParametersFileDateMask;
        if (string.IsNullOrWhiteSpace(parametersFileDateMask))
        {
            StShared.WriteErrorLine("parametersFileDateMask does not specified", true);
            return null;
        }

        var parametersFileExtension =
            project.ParametersFileExtension ?? supportToolsParameters.ParametersFileExtension;
        if (string.IsNullOrWhiteSpace(parametersFileExtension))
        {
            StShared.WriteErrorLine("parametersFileExtension does not specified", true);
            return null;
        }

        var localInstallerSettingsDomain = LocalInstallerSettingsDomain.Create(null, true,
            supportToolsParameters.LocalInstallerSettings, null, null, CancellationToken.None).Result;

        if (localInstallerSettingsDomain is null)
        {
            StShared.WriteErrorLine("LocalInstallerSettingsDomain does not Created", true);
            return null;
        }

        var programExchangeFileStorageName =
            supportToolsParameters.LocalInstallerSettings?.ProgramExchangeFileStorageName;

        if (string.IsNullOrWhiteSpace(programExchangeFileStorageName))
        {
            StShared.WriteErrorLine("LocalInstallerSettings.ProgramExchangeFileStorageName does not specified", true);
            return null;
        }

        var fileStorageForUpload =
            supportToolsParameters.GetFileStorageRequired(programExchangeFileStorageName);

        var installerBaseParameters = InstallerBaseParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (installerBaseParameters is null)
        {
            StShared.WriteErrorLine(
                $"installerBaseParameters does not created for project {projectName}/{environmentName} and server {serverName}",
                true);
            return null;
        }

        var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId, projectName,
            serverInfo);

        if (proxySettings is null)
            return null;

        var appSettingsUpdaterParameters = new AppSettingsUpdaterParameters(projectName, environmentName,
            project.ServiceName, installerBaseParameters, proxySettings, appSettingsEncoderParameters,
            checkVersionParameters.WebAgentForCheck, parametersFileDateMask, parametersFileExtension,
            fileStorageForUpload);
        return appSettingsUpdaterParameters;
    }
}