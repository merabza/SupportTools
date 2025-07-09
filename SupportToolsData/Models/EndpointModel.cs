using LibParameters;

namespace SupportToolsData.Models;

public class EndpointModel : ItemData
{
    public string EndpointName { get; set; }
    public string EndpointRoute { get; set; }
    public bool RequireAuthorization { get; set; }
    public EHttpMethod HttpMethod { get; set; }
    public EEndpointType EndpointType { get; set; }
    public string ReturnType { get; set; }
    public bool SendMessageToCurrentUser { get; set; }
}