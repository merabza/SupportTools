using LibFileParameters.Models;
using LibParameters;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.ToolCommandParameters;

public class ServiceInstallScriptCreatorParameters : IParameters
{
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

        return new ServiceInstallScriptCreatorParameters(supportToolsParameters.SecurityFolder, projectName, project,
            serverInfo, fileStorageForDownload);
    }

    private ServiceInstallScriptCreatorParameters(string? securityFolder, string projectName, ProjectModel project,
        ServerInfoModel serverInfo, FileStorageData fileStorageForExchange)
    {
        SecurityFolder = securityFolder;
        ProjectName = projectName;
        Project = project;
        ServerInfo = serverInfo;
        FileStorageForExchange = fileStorageForExchange;
    }

    public string? SecurityFolder { get; }
    public string ProjectName { get; }
    public ProjectModel Project { get; }
    public ServerInfoModel ServerInfo { get; }
    public FileStorageData FileStorageForExchange { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }
}