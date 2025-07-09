using LibParameters;

namespace SupportToolsData.Models;

public class RouteClassModel : ItemData
{
    public required string Root { get; set; } = "api";
    public required string Version { get; set; } = "v1";
    public required string Base { get; set; }

}