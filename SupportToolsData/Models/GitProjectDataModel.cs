using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SupportToolsData.Models;

public sealed class GitProjectDataModel
{
    public string? ProjectName => string.IsNullOrWhiteSpace(ProjectRelativePath)
        ? null
        : Path.GetFileNameWithoutExtension(ProjectRelativePath);

    public string? ProjectExtension => string.IsNullOrWhiteSpace(ProjectRelativePath)
        ? null
        : Path.GetExtension(ProjectRelativePath);

    public string? GitName { get; set; }
    public string? ProjectRelativePath { get; set; }
    public List<string> DependsOnProjectNames { get; set; } = new();

    public void DependsOn(List<string> dependsOnProjectNames)
    {
        var newProjects = dependsOnProjectNames.Except(DependsOnProjectNames).ToList();
        if (newProjects.Count > 0)
            DependsOnProjectNames.AddRange(newProjects);

        var forDeleteProjects = DependsOnProjectNames.Except(dependsOnProjectNames).ToList();
        if (forDeleteProjects.Count == 0)
            return;

        foreach (var forDeleteProject in forDeleteProjects)
            DependsOnProjectNames.Remove(forDeleteProject);
    }
}