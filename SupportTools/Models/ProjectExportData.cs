using System.Collections.Generic;
using LibGitData.Models;
using SupportToolsData.Models;

namespace SupportTools.Models;

public sealed class ProjectExportData
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectExportData(string projectName, ProjectModel project)
    {
        ProjectName = projectName;
        Project = project;
    }

    public string ProjectName { get; set; }
    public ProjectModel Project { get; set; }
    public Dictionary<string, GitDataModel> Gits { get; init; } = new();
}
