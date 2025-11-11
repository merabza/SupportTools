using LibParameters;

namespace SupportToolsData.Models;

public class DotnetToolData : ItemData
{
    public string? PackageId { get; set; }
    public string? InstalledVersion { get; set; }
    public string? LatestVersion { get; set; }
    public string? MaxVersion { get; set; }
    public string? CommandName { get; set; }
    public string? Description { get; set; }

    public override string? GetItemKey()
    {
        return Description;
    }
}