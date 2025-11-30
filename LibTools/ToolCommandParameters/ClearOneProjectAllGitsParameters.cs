using System.Collections.Generic;
using System.Linq;
using LibGitData;
using LibGitData.Models;
using LibGitWork;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;

namespace LibTools.ToolCommandParameters;

public sealed class ClearOneProjectAllGitsParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    private ClearOneProjectAllGitsParameters(string gitsFolder, List<StsGitDataModel> gitData)
    {
        //ProjectName = projectName;
        GitData = gitData;
        GitsFolder = gitsFolder;
    }

    //public string? ProjectName { get; }
    public List<StsGitDataModel> GitData { get; }
    public string GitsFolder { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }

    public static ClearOneProjectAllGitsParameters? Create(ILogger? logger,
        SupportToolsParameters supportToolsParameters, string projectName, EGitCol gitCol)
    {
        var project = supportToolsParameters.GetProject(projectName);

        if (project == null)
        {
            StShared.WriteErrorLine("project is not found", true);
            return null;
        }

        var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);

        if (gitsFolder == null)
        {
            StShared.WriteErrorLine("Gits folder is not found", true);
            return null;
        }

        var gitProjectNames = gitCol switch
        {
            EGitCol.Main => project.GitProjectNames,
            EGitCol.ScaffoldSeed => project.ScaffoldSeederGitProjectNames,
            _ => null
        } ?? [];

        var gitProjects = GitProjects.Create(logger, supportToolsParameters.GitProjects);

        var gitRepos = GitRepos.Create(logger, supportToolsParameters.Gits,
            project.SpaProjectFolderRelativePath(gitProjects), true, false);

        var absentGitRepoNames = gitProjectNames.Except(gitRepos.Gits.Keys).ToList();

        if (absentGitRepoNames.Count != 0)
        {
            foreach (var absentGitRepoName in absentGitRepoNames)
                StShared.WriteErrorLine(absentGitRepoName, true, null, false);
            StShared.WriteErrorLine("Gits with this names are absent", true);
        }

        return new ClearOneProjectAllGitsParameters(gitsFolder,
            gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value).ToList());
    }
}