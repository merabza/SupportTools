using System.Collections.Generic;
using System.IO;

namespace SupportToolsData.Domain;

public sealed class GitProjectDataDomain
{
    public GitProjectDataDomain(string gitName, string projectRelativePath, List<string> dependsOnProjectNames)
    {
        GitName = gitName;
        ProjectRelativePath = projectRelativePath;
        DependsOnProjectNames = dependsOnProjectNames;
    }

    public string ProjectName => Path.GetFileNameWithoutExtension(ProjectRelativePath);

    public string GitName { get; set; }
    public string ProjectRelativePath { get; set; }
    public List<string> DependsOnProjectNames { get; set; }
}