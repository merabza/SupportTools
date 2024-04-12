using System.Collections.Generic;
using System.Linq;
using LibParameters;
using SupportToolsData.Models;
using SupportToolsData;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
using SupportToolsData.Domain;

namespace LibGitWork.ToolCommandParameters;

public class SyncOneProjectAllGitsParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsParameters(string gitsFolder, List<GitDataDomain> gitData, List<string>? changedGitProjects, EGitCollect? gitCollect)
    {
        GitData = gitData;
        ChangedGitProjects = changedGitProjects;
        GitCollect = gitCollect;
        GitsFolder = gitsFolder;
    }

    public List<GitDataDomain> GitData { get; }
    public List<string>? ChangedGitProjects { get; }
    public EGitCollect? GitCollect { get; }
    public string GitsFolder { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }


    public static SyncOneProjectAllGitsParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName, EGitCol gitCol, List<string>? changedGitProjects, EGitCollect? gitCollect)
    {

        var project = supportToolsParameters.GetProject(projectName);

        if (project == null)
        {
            StShared.WriteErrorLine("project does not found", true);
            return null;
        }

        var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);

        if (gitsFolder == null)
        {
            StShared.WriteErrorLine("Gits folder does not found", true);
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
            project.MainProjectFolderRelativePath(gitProjects),
            project.SpaProjectFolderRelativePath(gitProjects));

        var absentGitRepoNames = gitProjectNames.Except(gitRepos.Gits.Keys).ToList();


        if (absentGitRepoNames.Count != 0)
        {
            foreach (var absentGitRepoName in absentGitRepoNames)
                StShared.WriteErrorLine(absentGitRepoName, true, null, false);
            StShared.WriteErrorLine("Gits with this names are absent", true);
        }

        return new SyncOneProjectAllGitsParameters(gitsFolder,
            gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value).ToList(), changedGitProjects,
            gitCollect);

    }
}