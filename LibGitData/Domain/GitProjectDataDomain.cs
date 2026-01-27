using System.Collections.Generic;
using System.IO;

namespace LibGitData.Domain;

public sealed class GitProjectDataDomain
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectDataDomain(string gitName, string projectRelativePath, string projectFileName,
        List<string> dependsOnProjectNames)
    {
        GitName = gitName;
        ProjectRelativePath = projectRelativePath;
        DependsOnProjectNames = dependsOnProjectNames;
        ProjectFileName = projectFileName;
    }

    public string ProjectName => Path.GetFileNameWithoutExtension(ProjectFileName);

    public override string ToString()
    {
        return GitName;
    }

    public string GitName { get; set; }
    public string ProjectRelativePath { get; set; }
    public string ProjectFileName { get; set; }
    public List<string> DependsOnProjectNames { get; set; }
}