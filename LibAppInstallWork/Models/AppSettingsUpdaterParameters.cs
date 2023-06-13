//Created by ProjectParametersClassCreator at 1/11/2021 20:04:36

using Installer.Domain;
using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class AppSettingsUpdaterParameters : IParameters
{
    private AppSettingsUpdaterParameters(string projectName, string? serviceName,
        InstallerBaseParameters installerBaseParameters, ProxySettingsBase proxySettings,
        AppSettingsEncoderParameters appSettingsEncoderParameters, ApiClientSettingsDomain webAgentForCheck,
        string parametersFileDateMask, string parametersFileExtension, FileStorageData fileStorageForUpload)
    {
        ProjectName = projectName;
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
        string projectName, string serverName)
    {
        var checkVersionParameters =
            CheckVersionParameters.Create(supportToolsParameters, projectName, serverName);
        if (checkVersionParameters is null)
            return null;

        var serviceStartStopParameters =
            ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverName);
        if (serviceStartStopParameters is null)
            return null;

        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        var serverInfo = project.GetServerInfoRequired(serverName);

        var appSettingsEncoderParameters =
            AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverName);
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

        var localInstallerSettingsDomain =
            LocalInstallerSettingsDomain.Create(null, true, supportToolsParameters.LocalInstallerSettings);

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

        //if (string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
        //{
        //    StShared.WriteErrorLine(
        //        $"ApiVersionId does not specified for server {serverName} and project {projectName}", true);
        //    return null;
        //}

        var fileStorageForUpload =
            supportToolsParameters.GetFileStorageRequired(programExchangeFileStorageName);

        var installerBaseParameters =
            InstallerBaseParameters.Create(supportToolsParameters, projectName, serverName);
        if (installerBaseParameters is null)
        {
            StShared.WriteErrorLine(
                $"installerBaseParameters does not created for project {projectName} and server {serverName}", true);
            return null;
        }

        var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId, projectName,
            serverName);

        if (proxySettings is null)
            return null;

        var appSettingsUpdaterParameters = new AppSettingsUpdaterParameters(projectName,
            project.ServiceName, installerBaseParameters, proxySettings,
            appSettingsEncoderParameters, checkVersionParameters.WebAgentForCheck, parametersFileDateMask,
            parametersFileExtension, fileStorageForUpload);
        return appSettingsUpdaterParameters;
    }
}