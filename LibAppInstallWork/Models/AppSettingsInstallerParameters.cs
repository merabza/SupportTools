using ApiClientsManagement;
using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class AppSettingsInstallerParameters : IParameters
{
    private AppSettingsInstallerParameters(string projectName, ServerInfoModel serverInfo,
        InstallerBaseParameters installerBaseParameters, string appSettingsEncodedJsonFileName,
        FileStorageData fileStorageForUpload, FileStorageData fileStorageForDownload,
        ApiClientSettingsDomain webAgentForCheck, ProxySettingsBase proxySettings, string parametersFileDateMask,
        string parametersFileExtension)
    {
        ProjectName = projectName;
        ServerInfo = serverInfo;
        InstallerBaseParameters = installerBaseParameters;
        AppSettingsEncodedJsonFileName = appSettingsEncodedJsonFileName;
        FileStorageForUpload = fileStorageForUpload;
        FileStorageForDownload = fileStorageForDownload;
        WebAgentForCheck = webAgentForCheck;
        ProxySettings = proxySettings;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
    }

    public string ProjectName { get; }
    public ServerInfoModel ServerInfo { get; }
    public InstallerBaseParameters InstallerBaseParameters { get; }
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
        string projectName, ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);
        var environmentName = serverInfo.EnvironmentName;

        if (!project.IsService)
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

        if (serverInfo.AppSettingsEncodedJsonFileName is null)
        {
            StShared.WriteErrorLine(
                $"Project AppSettingsEncodedJsonFileName does not specified for project {projectName}/{environmentName} and server {serverInfo.GetItemKey()}",
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

        var fileStorageForUpload = supportToolsParameters.GetFileStorageRequired(programExchangeFileStorageName);
        var fileStorageForDownload = supportToolsParameters.GetFileStorageRequired(programExchangeFileStorageName);

        var parametersFileDateMask = project.ParametersFileDateMask ?? supportToolsParameters.ParametersFileDateMask;
        if (string.IsNullOrWhiteSpace(parametersFileDateMask))
        {
            StShared.WriteErrorLine("parametersFileDateMask does not specified", true);
            return null;
        }

        var parametersFileExtension = project.ParametersFileExtension ?? supportToolsParameters.ParametersFileExtension;
        if (string.IsNullOrWhiteSpace(parametersFileExtension))
        {
            StShared.WriteErrorLine("parametersFileExtension does not specified", true);
            return null;
        }

        var installerBaseParameters = InstallerBaseParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (installerBaseParameters is null)
        {
            StShared.WriteErrorLine(
                $"installerBaseParameters does not created for project {projectName} and server {serverInfo.GetItemKey()}",
                true);
            return null;
        }

        var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId, projectName,
            serverInfo);

        if (proxySettings is null)
            return null;

        var appSettingsInstallerParameters = new AppSettingsInstallerParameters(projectName, serverInfo,
            installerBaseParameters, serverInfo.AppSettingsEncodedJsonFileName, fileStorageForUpload,
            fileStorageForDownload, webAgentForCheck, proxySettings, parametersFileDateMask, parametersFileExtension);
        return appSettingsInstallerParameters;
    }
}