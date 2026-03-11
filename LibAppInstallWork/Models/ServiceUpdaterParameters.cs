using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ServiceUpdaterParameters : IParameters
{
    private ServiceUpdaterParameters(string serviceUserName, InstallerBaseParameters installerBaseParameters,
        ProgramPublisherParameters progPublisherParameters, CheckVersionParameters checkVersionParameters,
        ProxySettingsBase proxySettings, AppSettingsEncoderParameters appSettingsEncoderParameters,
        string programArchiveDateMask, string programArchiveExtension, string parametersFileDateMask,
        string parametersFileExtension, FileStorageData fileStorageForDownload, string? serviceDescriptionSignature,
        string? projectDescription)
    {
        ServiceUserName = serviceUserName;
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
        ServiceDescriptionSignature = serviceDescriptionSignature;
        ProjectDescription = projectDescription;
    }

    public string ServiceUserName { get; }
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
    public string? ServiceDescriptionSignature { get; }
    public string? ProjectDescription { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static async ValueTask<ServiceUpdaterParameters?> Create(ILogger logger,
        SupportToolsParameters supportToolsParameters, string projectName, ServerInfoModel serverInfo,
        CancellationToken cancellationToken = default)
    {
        var checkVersionParameters = CheckVersionParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (checkVersionParameters is null)
        {
            return null;
        }

        var serviceStartStopParameters =
            ServiceStartStopParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (serviceStartStopParameters is null)
        {
            return null;
        }

        ProjectModel project = supportToolsParameters.GetProjectRequired(projectName);

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        var progPublisherParameters =
            ProgramPublisherParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
        if (progPublisherParameters == null)
        {
            return null;
        }

        var appSettingsEncoderParameters =
            AppSettingsEncoderParameters.Create(supportToolsParameters, projectName, serverInfo);
        if (appSettingsEncoderParameters == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(serverInfo.ServiceUserName))
        {
            StShared.WriteErrorLine(
                $"ServiceUserName does not specified for server {serverInfo.GetItemKey()} and project {projectName}",
                true);
            return null;
        }

        string? programArchiveDateMask =
            project.ProgramArchiveDateMask ?? supportToolsParameters.ProgramArchiveDateMask;
        if (string.IsNullOrWhiteSpace(programArchiveDateMask))
        {
            StShared.WriteErrorLine("programArchiveDateMask does not specified", true);
            return null;
        }

        string? programArchiveExtension =
            project.ProgramArchiveExtension ?? supportToolsParameters.ProgramArchiveExtension;
        if (string.IsNullOrWhiteSpace(programArchiveExtension))
        {
            StShared.WriteErrorLine("programArchiveExtension does not specified", true);
            return null;
        }

        string? parametersFileDateMask =
            project.ParametersFileDateMask ?? supportToolsParameters.ParametersFileDateMask;
        if (string.IsNullOrWhiteSpace(parametersFileDateMask))
        {
            StShared.WriteErrorLine("parametersFileDateMask does not specified", true);
            return null;
        }

        string? parametersFileExtension =
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

        FileStorageData fileStorageForUpload =
            supportToolsParameters.GetFileStorageRequired(supportToolsParameters.FileStorageNameForExchange);

        var installerBaseParameters =
            await InstallerBaseParameters.Create(supportToolsParameters, projectName, serverInfo, cancellationToken);
        if (installerBaseParameters is null)
        {
            StShared.WriteErrorLine(
                $"installerBaseParameters does not created for project {projectName} and server {serverInfo.GetItemKey()}",
                true);
            return null;
        }

        ProxySettingsBase? proxySettings = ProxySettingsCreator.Create(serverInfo.ServerSidePort,
            serverInfo.ApiVersionId, projectName, serverInfo);

        if (proxySettings is null)
        {
            return null;
        }

        var programServiceUpdaterParameters = new ServiceUpdaterParameters(serverInfo.ServiceUserName,
            installerBaseParameters, progPublisherParameters, checkVersionParameters, proxySettings,
            appSettingsEncoderParameters, programArchiveDateMask, programArchiveExtension, parametersFileDateMask,
            parametersFileExtension, fileStorageForUpload, supportToolsParameters.ServiceDescriptionSignature,
            project.ProjectDescription);
        return programServiceUpdaterParameters;
    }
}
