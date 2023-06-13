namespace LibAppInstallWork.Models;

public sealed class ProxySettings : ProxySettingsBase
{
    public ProxySettings(int serverSidePort, string apiVersionId)
    {
        ServerSidePort = serverSidePort;
        ApiVersionId = apiVersionId;
    }

    public int ServerSidePort { get; set; }
    public string ApiVersionId { get; set; }
}