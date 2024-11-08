namespace LibAppInstallWork.Models;

public sealed class ProxySettings : ProxySettingsBase
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProxySettings(int serverSidePort, string apiVersionId)
    {
        ServerSidePort = serverSidePort;
        ApiVersionId = apiVersionId;
    }

    public int ServerSidePort { get; set; }
    public string ApiVersionId { get; set; }
}