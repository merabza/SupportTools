using SupportToolsData.Models;

namespace LibAppInstallWork.Models;

internal sealed class ProjectServerParameters
{
    public ProjectServerParameters(ProjectModel project, ServerInfoModel serverInfo, ServerDataModel serverData)
    {
        Project = project;
        ServerInfo = serverInfo;
        ServerData = serverData;
    }

    public ProjectModel Project { get; }
    public ServerInfoModel ServerInfo { get; }
    public ServerDataModel ServerData { get; }

    public static ProjectServerParameters Create(SupportToolsParameters supportToolsParameters, string projectName,
        string serverName)
    {
        var project = supportToolsParameters.GetProjectRequired(projectName);
        var serverInfo = project.GetServerInfoRequired(serverName);
        var serverData = supportToolsParameters.GetServerDataRequired(serverName);
        return new ProjectServerParameters(project, serverInfo, serverData);
    }
}