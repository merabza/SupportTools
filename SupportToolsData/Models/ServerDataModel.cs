using SystemTools.SystemToolsShared;

namespace SupportToolsData.Models;

public sealed class ServerDataModel : ItemData
{
    public bool IsLocal { get; set; }
    public string? WebAgentName { get; set; }
    public string? WebAgentInstallerName { get; set; }
    public string? FilesUserName { get; set; }
    public string? FilesUsersGroupName { get; set; }
    public string? Runtime { get; set; }
    public string? ServerSideDownloadFolder { get; set; }
    public string? ServerSideDeployFolder { get; set; }
}
