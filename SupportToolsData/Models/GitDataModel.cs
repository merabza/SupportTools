using LibParameters;

namespace SupportToolsData.Models;

public sealed class GitDataModel : ItemData
{
    public const string MainProjectFolderRelativePathName = "{MainProjectFolderRelativePath}";
    public const string SpaProjectFolderRelativePathName = "{SpaProjectFolderRelativePath}";
    public string? GitProjectAddress { get; set; }
    public string? GitProjectFolderName { get; set; }
}