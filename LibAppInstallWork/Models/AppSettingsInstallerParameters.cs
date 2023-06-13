using Installer.Domain;
using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class AppSettingsInstallerParameters : IParameters
{
    private AppSettingsInstallerParameters(string projectName, string serverName,
        InstallerBaseParameters installerBaseParameters, string serviceName, string appSettingsEncodedJsonFileName,
        FileStorageData fileStorageForUpload, FileStorageData fileStorageForDownload,
        ApiClientSettingsDomain webAgentForCheck, ProxySettingsBase proxySettings,
        string parametersFileDateMask, string parametersFileExtension)
    {
        ProjectName = projectName;
        ServerName = serverName;
        InstallerBaseParameters = installerBaseParameters;
        ServiceName = serviceName;
        AppSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
        FileStorageForUpload = fileStorageForUpload;
        FileStorageForDownload = fileStorageForDownload;
        WebAgentForCheck = webAgentForCheck;
        ProxySettings = proxySettings;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
    }

    public string ProjectName { get; }
    public string ServerName { get; }
    public InstallerBaseParameters InstallerBaseParameters { get; }
    public string ServiceName { get; }
    public string AppSettingsEncodedJsonFileName { get; }
    public FileStorageData FileStorageForUpload { get; }
    public FileStorageData FileStorageForDownload { get; }
    public ApiClientSettingsDomain WebAgentForCheck { get; }
    public ProxySettingsBase ProxySettings { get; }
    public string ParametersFileDateMask { get; }
    public string ParametersFileExtension { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static AppSettingsInstallerParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, string serverName)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        var serverInfo = project.GetServerInfoRequired(serverName);

        var webAgentNameForCheck = serverInfo.WebAgentNameForCheck;
        if (string.IsNullOrWhiteSpace(webAgentNameForCheck))
        {
            StShared.WriteErrorLine(
                $"webAgentNameForCheck does not specified for Project {projectName} and server {serverName}",
                true);
            return null;
        }

        var webAgentForCheck = supportToolsParameters.GetWebAgentRequired(webAgentNameForCheck);

        if (project.ServiceName is null)
        {
            StShared.WriteErrorLine($"Project ServiceName does not specified for project {projectName}", true);
            return null;
        }

        if (serverInfo.AppSettingsEncodedJsonFileName is null)
        {
            StShared.WriteErrorLine(
                $"Project AppSettingsEncodedJsonFileName does not specified for project {projectName} and server {serverName}",
                true);
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
        var fileStorageForDownload =
            supportToolsParameters.GetFileStorageRequired(programExchangeFileStorageName);

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

        //if (string.IsNullOrWhiteSpace(serverInfo.ApiVersionId))
        //{
        //    StShared.WriteErrorLine(
        //        $"ApiVersionId does not specified for server {serverName} and project {projectName}", true);
        //    return null;
        //}

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

        var appSettingsInstallerParameters = new AppSettingsInstallerParameters(projectName,
            serverName, installerBaseParameters, project.ServiceName, serverInfo.AppSettingsEncodedJsonFileName,
            fileStorageForUpload, fileStorageForDownload, webAgentForCheck, proxySettings, parametersFileDateMask,
            parametersFileExtension);
        return appSettingsInstallerParameters;
    }
}