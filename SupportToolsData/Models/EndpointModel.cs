using LibParameters;

namespace SupportToolsData.Models;

public class EndpointModel : ItemData
{
    public required string Root { get; set; } = "api";
    public required string Version { get; set; } = "v1";
    public required string Base { get; set; }
    public required string EndpointName { get; set; }
    public required string EndpointRoute { get; set; }
    public bool RequireAuthorization { get; set; }
    public required string HttpMethod { get; set; }
    public required EEndpointType EndpointType { get; set; }
    public required string ReturnType { get; set; }
    public bool SendMessageToCurrentUser { get; set; }
}