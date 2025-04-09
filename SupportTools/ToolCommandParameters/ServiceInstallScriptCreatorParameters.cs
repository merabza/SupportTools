using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.ToolCommandParameters;

public sealed class ServiceInstallScriptCreatorParameters : IParameters
{
    private ServiceInstallScriptCreatorParameters(string? securityFolder, string projectName,
        string? serviceDescriptionSignature, ProjectModel project, ServerInfoModel serverInfo,
        FileStorageData fileStorageForExchange)
    {
        SecurityFolder = securityFolder;
        ProjectName = projectName;
        Project = project;
        ServiceDescriptionSignature = serviceDescriptionSignature;
        ServerInfo = serverInfo;
        FileStorageForExchange = fileStorageForExchange;
    }

    public string? SecurityFolder { get; }
    public string ProjectName { get; }
    public string? ServiceDescriptionSignature { get; }

    public ProjectModel Project { get; }
    public ServerInfoModel ServerInfo { get; }
    public FileStorageData FileStorageForExchange { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ServiceInstallScriptCreatorParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (!project.IsService)
        {
            StShared.WriteErrorLine($"Project {projectName} is not service", true);
            return null;
        }

        if (string.IsNullOrWhiteSpace(supportToolsParameters.FileStorageNameForExchange))
        {
            StShared.WriteErrorLine("supportToolsParameters.FileStorageNameForExchange does not specified", true);
            return null;
        }

        var fileStorageForDownload =
            supportToolsParameters.GetFileStorageRequired(supportToolsParameters.FileStorageNameForExchange);

        return new ServiceInstallScriptCreatorParameters(supportToolsParameters.SecurityFolder, projectName,
            supportToolsParameters.ServiceDescriptionSignature, project, serverInfo, fileStorageForDownload);
    }
}