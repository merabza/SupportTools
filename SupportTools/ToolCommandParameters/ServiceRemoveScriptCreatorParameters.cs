using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.ToolCommandParameters;

public sealed class ServiceRemoveScriptCreatorParameters : IParameters
{
    private ServiceRemoveScriptCreatorParameters(string? securityFolder, string projectName, ServerInfoModel serverInfo,
        ProjectModel project)
    {
        SecurityFolder = securityFolder;
        ProjectName = projectName;
        ServerInfo = serverInfo;
        Project = project;
    }

    public string? SecurityFolder { get; }
    public string ProjectName { get; }
    public ProjectModel Project { get; }
    public ServerInfoModel ServerInfo { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ServiceRemoveScriptCreatorParameters? Create(SupportToolsParameters supportToolsParameters,
        string projectName, ServerInfoModel serverInfo)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);

        if (project.IsService)
            return new ServiceRemoveScriptCreatorParameters(supportToolsParameters.SecurityFolder, projectName,
                serverInfo, project);

        StShared.WriteErrorLine($"Project {projectName} is not service", true);
        return null;
    }
}