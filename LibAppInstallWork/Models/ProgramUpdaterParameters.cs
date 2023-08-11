using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppInstallWork.Models;

public sealed class ProgramUpdaterParameters : IParameters
{
    private ProgramUpdaterParameters(InstallerBaseParameters installerBaseParameters,
        ProgramPublisherParameters programPublisherParameters, string programArchiveDateMask,
        string programArchiveExtension, string parametersFileDateMask, string parametersFileExtension,
        FileStorageData fileStorageForDownload)
    {
        InstallerBaseParameters = installerBaseParameters;
        ProgramPublisherParameters = programPublisherParameters;
        ProgramArchiveDateMask = programArchiveDateMask;
        ProgramArchiveExtension = programArchiveExtension;
        ParametersFileDateMask = parametersFileDateMask;
        ParametersFileExtension = parametersFileExtension;
        FileStorageForDownload = fileStorageForDownload;
    }

    public ProgramPublisherParameters ProgramPublisherParameters { get; }
    public InstallerBaseParameters InstallerBaseParameters { get; }
    public FileStorageData FileStorageForDownload { get; }
    public string ProgramArchiveDateMask { get; }
    public string ProgramArchiveExtension { get; }
    public string ParametersFileDateMask { get; }
    public string ParametersFileExtension { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ProgramUpdaterParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        var programPublisherParameters =
            ProgramPublisherParameters.Create(logger, supportToolsParameters, projectName, serverInfo);
        if (programPublisherParameters == null)
            return null;

        var programArchiveDateMask = project.ProgramArchiveDateMask ?? supportToolsParameters.ProgramArchiveDateMask;
        if (string.IsNullOrWhiteSpace(programArchiveDateMask))
        {
            StShared.WriteErrorLine("programArchiveDateMask does not specified", true);
            return null;
        }

        var programArchiveExtension = project.ProgramArchiveExtension ?? supportToolsParameters.ProgramArchiveExtension;
        if (string.IsNullOrWhiteSpace(programArchiveExtension))
        {
            StShared.WriteErrorLine("programArchiveExtension does not specified", true);
            return null;
        }

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

        var programParameters = new ProgramUpdaterParameters(installerBaseParameters, programPublisherParameters,
            programArchiveDateMask, programArchiveExtension, parametersFileDateMask, parametersFileExtension,
            fileStorageForUpload);
        return programParameters;
    }
}