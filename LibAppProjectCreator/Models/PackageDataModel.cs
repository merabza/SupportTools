namespace LibAppProjectCreator.Models;

public sealed class PackageDataModel
{
    public PackageDataModel(string projectFilePath, string packageName, string? version)
    {
        ProjectFilePath = projectFilePath;
        PackageName = packageName;
        Version = version;
    }

    public string ProjectFilePath { get; set; }
    public string PackageName { get; set; }
    public string? Version { get; set; }
}