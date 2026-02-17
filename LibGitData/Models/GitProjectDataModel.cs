using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibGitData.Models;

public sealed class GitProjectDataModel
{
    public string? ProjectExtension =>
        string.IsNullOrWhiteSpace(ProjectFileName) ? null : Path.GetExtension(ProjectFileName);

    public string? GitName { get; set; }
    public string? ProjectRelativePath { get; set; }
    public string? ProjectFileName { get; set; }

    public List<string> DependsOnProjectNames { get; set; } = [];

    public void DependsOn(List<string> dependsOnProjectNames)
    {
        List<string> newProjects = dependsOnProjectNames.Except(DependsOnProjectNames).ToList();
        if (newProjects.Count > 0)
        {
            DependsOnProjectNames.AddRange(newProjects);
        }

        List<string> forDeleteProjects = DependsOnProjectNames.Except(dependsOnProjectNames).ToList();
        if (forDeleteProjects.Count == 0)
        {
            return;
        }

        foreach (string forDeleteProject in forDeleteProjects)
        {
            DependsOnProjectNames.Remove(forDeleteProject);
        }
    }
}
