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
    public SyncOneProjectAllGitsParameters(string? projectName, string gitsFolder, List<GitDataDomain> gitData,
        Dictionary<EGitCollect, Dictionary<string, List<string>>>? changedGitProjects, bool isFirstSync)
    {
        ProjectName = projectName;
        GitData = gitData;
        ChangedGitProjects = changedGitProjects;
        IsFirstSync = isFirstSync;
        GitsFolder = gitsFolder;
    }

    public string? ProjectName { get; }
    public List<GitDataDomain> GitData { get; }
    public Dictionary<EGitCollect, Dictionary<string, List<string>>>? ChangedGitProjects { get; }
    public string GitsFolder { get; }
    public bool IsFirstSync { get; }

    public bool CheckBeforeSave()
    {
        return true;
    }


    public static SyncOneProjectAllGitsParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName, EGitCol gitCol,
        Dictionary<EGitCollect, Dictionary<string, List<string>>>? changedGitProjects, bool isFirstSync)
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

        return new SyncOneProjectAllGitsParameters(projectName, gitsFolder,
            gitRepos.Gits.Where(x => gitProjectNames.Contains(x.Key)).Select(x => x.Value).ToList(), changedGitProjects,
            isFirstSync);

    }
}