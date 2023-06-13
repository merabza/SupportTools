using System.Collections.Generic;
using SupportToolsData.Models;

namespace SupportTools.Models;

public sealed class ProjectExportData
{
    public ProjectExportData(string projectName, ProjectModel project)
    {
        ProjectName = projectName;
        Project = project;
    }

    public string ProjectName { get; set; }
    public ProjectModel Project { get; set; }
    public Dictionary<string, GitDataModel> Gits { get; init; } = new();
}