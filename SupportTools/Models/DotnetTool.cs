namespace SupportTools.Models;

public class DotnetTool
{
    public DotnetTool(string packageId, string version, string? availableVersion, string commands)
    {
        PackageId = packageId;
        Version = version;
        AvailableVersion = availableVersion;
        Commands = commands;
    }

    public string PackageId { get; }
    public string Version { get; }
    public string? AvailableVersion { get; set; }
    public string Commands { get; }
}