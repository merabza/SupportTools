using Installer.Domain;
using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ProgramInstallerParameters : IParameters
{
    private ProgramInstallerParameters(string projectName, ServerInfoModel serverInfo, string? serviceName,
        InstallerBaseParameters installerBaseParameters, string serviceUserName, string appSettingsJsonSourceFileName,
        string encodedJsonFileName, ProxySettingsBase proxySettings, FileStorageData fileStorageForExchange,
        ApiClientSettingsDomain webAgentForCheck, string programArchiveDateMask, string programArchiveExtension,
        string parametersFileDateMask, string parametersFileExtension)
    {
        ProjectName = projectName;
        ServerInfo = serverInfo;
        ServiceName = serviceName;
        InstallerBaseParameters = installerBaseParameters;
        ServiceUserName = serviceUserName;
        AppSettingsJsonSourceFileName = appSettingsJsonSourceFileName;
        EncodedJsonFileName = encodedJsonFileName;
        ProxySettings = proxySettings;
        FileStorageForExchange = fileStorageForExchange;
        WebAgentForCheck = webAgentForCheck;
        ProgramArchiveDateMask = programArchiveDateMask;
        ProgramArchiveExtension = programArchiveExtension;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
    }

    public string ProjectName { get; }
    public ServerInfoModel ServerInfo { get; }
    public string? ServiceName { get; }
    public InstallerBaseParameters InstallerBaseParameters { get; }

    public string AppSettingsJsonSourceFileName { get; }
    public ApiClientSettingsDomain WebAgentForCheck { get; }

    public string ProgramArchiveDateMask { get; }
    public string ProgramArchiveExtension { get; }
    public string ParametersFileDateMask { get; }

    public string ParametersFileExtension { get; }

    public string ServiceUserName { get; }
    public string EncodedJsonFileName { get; }

    public ProxySettingsBase ProxySettings { get; }

    public FileStorageData FileStorageForExchange { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ProgramInstallerParameters? Create(SupportToolsParameters supportToolsParameters, string projectName,
        ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.ServiceUserName))
        {
            StShared.WriteErrorLine(
                $"ServiceUserName does not specified for server {serverInfo.GetItemKey()} and project {projectName}",
                true);
            return null;
        }

        if (serverInfo.AppSettingsJsonSourceFileName is null)
        {
            StShared.WriteErrorLine(
                $"AppSettingsJsonSourceFileName does not specified for project {projectName} and server {serverInfo.GetItemKey()}",
                true);
            return null;
        }

        if (serverInfo.AppSettingsEncodedJsonFileName is null)
        {
            StShared.WriteErrorLine(
                $"AppSettingsEncodedJsonFileName does not specified for project {projectName} and server {serverInfo.GetItemKey()}",
                true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.FileStorageNameForExchange))
        {
            StShared.WriteErrorLine("supportToolsParameters.FileStorageNameForExchange does not specified", true);
            return null;
        }

        var fileStorageForDownload =
            supportToolsParameters.GetFileStorageRequired(supportToolsParameters.FileStorageNameForExchange);

        var programArchiveDateMask =
            project.ProgramArchiveDateMask ?? supportToolsParameters.ProgramArchiveDateMask;
        if (string.IsNullOrWhiteSpace(programArchiveDateMask))
        {
            StShared.WriteErrorLine("programArchiveDateMask does not specified", true);
            return null;
        }

        var programArchiveExtension =
            project.ProgramArchiveExtension ?? supportToolsParameters.ProgramArchiveExtension;
        if (string.IsNullOrWhiteSpace(programArchiveExtension))
        {
            StShared.WriteErrorLine("programArchiveExtension does not specified", true);
            return null;
        }

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

        var installerBaseParameters =
            InstallerBaseParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (installerBaseParameters is null)
        {
            StShared.WriteErrorLine(
                $"installerBaseParameters does not created for project {projectName} and server {serverInfo.GetItemKey()}",
                true);
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

        var proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort, serverInfo.ApiVersionId, projectName,
            serverInfo);

        if (proxySettings is null)
            return null;

        var progInstallerParameters = new ProgramInstallerParameters(projectName, serverInfo, project.ServiceName,
            installerBaseParameters, serverInfo.ServiceUserName, serverInfo.AppSettingsJsonSourceFileName,
            serverInfo.AppSettingsEncodedJsonFileName, proxySettings, fileStorageForDownload, webAgentForCheck,
            programArchiveDateMask, programArchiveExtension, parametersFileDateMask, parametersFileExtension);
        return progInstallerParameters;
    }
}