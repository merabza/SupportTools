using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ServiceUpdaterParameters : IParameters
{
    private ServiceUpdaterParameters(string serviceUserName, string serviceName,
        InstallerBaseParameters installerBaseParameters, ProgramPublisherParameters progPublisherParameters,
        CheckVersionParameters checkVersionParameters, ProxySettingsBase proxySettings,
        AppSettingsEncoderParameters appSettingsEncoderParameters, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        FileStorageData fileStorageForDownload)
    {
        ServiceUserName = serviceUserName;
        ServiceName = serviceName;
        InstallerBaseParameters = installerBaseParameters;
        ProgramPublisherParameters = progPublisherParameters;
        CheckVersionParameters = checkVersionParameters;
        ProxySettings = proxySettings;
        AppSettingsEncoderParameters = appSettingsEncoderParameters;
        ProgramArchiveDateMask = programArchiveDateMask;
        ProgramArchiveExtension = programArchiveExtension;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
        FileStorageForDownload = fileStorageForDownload;
    }

    public string ServiceUserName { get; }
    public string ServiceName { get; }
    public ProgramPublisherParameters ProgramPublisherParameters { get; }
    public InstallerBaseParameters InstallerBaseParameters { get; }
    public CheckVersionParameters CheckVersionParameters { get; }
    public ProxySettingsBase ProxySettings { get; }
    public AppSettingsEncoderParameters AppSettingsEncoderParameters { get; }
    public FileStorageData FileStorageForDownload { get; }
    public string ProgramArchiveDateMask { get; }
    public string ProgramArchiveExtension { get; }
    public string ParametersFileDateMask { get; }
    public string ParametersFileExtension { get; }

    public bool IsService => !string.IsNullOrWhiteSpace(ServiceName);

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ServiceUpdaterParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
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

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        var progPublisherParameters =
            ProgramPublisherParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
        if (progPublisherParameters == null)
            return null;

        var appSettingsEncoderParameters =
            AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (appSettingsEncoderParameters == null)
            return null;

        if (string.IsNullOrWhiteSpace(serverInfo.ServiceUserName))
        {
            StShared.WriteErrorLine(
                $"ServiceUserName does not specified for server {serverInfo.GetItemKey()} and project {projectName}",
                true);
            return null;
        }

        if (project.ServiceName is null)
        {
            StShared.WriteErrorLine($"Project ServiceName does not specified for project {projectName}", true);
            return null;
        }

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

        if (string.IsNullOrWhiteSpace(supportToolsParameters.FileStorageNameForExchange))
        {
            StShared.WriteErrorLine($"FileStorageNameForExchange does not specified for Project {projectName}", true);
            return null;
        }

        var fileStorageForUpload =
            supportToolsParameters.GetFileStorageRequired(supportToolsParameters.FileStorageNameForExchange);

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

        var programServiceUpdaterParameters = new ServiceUpdaterParameters(
            serverInfo.ServiceUserName,
            project.ServiceName, installerBaseParameters, progPublisherParameters, checkVersionParameters,
            proxySettings, appSettingsEncoderParameters, programArchiveDateMask,
            programArchiveExtension, parametersFileDateMask, parametersFileExtension, fileStorageForUpload);
        return programServiceUpdaterParameters;
    }
}