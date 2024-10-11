using LibGitData;
using LibGitData.Domain;
using LibGitData.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibGitWork.ToolCommandParameters;

public class GitSyncParameters : IParameters
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSyncParameters(GitDataDomain gitData, string gitsFolder)
    {
        GitData = gitData;
        GitsFolder = gitsFolder;
    }

    public GitDataDomain GitData { get; }
    public string GitsFolder { get; }


    public bool CheckBeforeSave()
    {
        return true;
    }

    public static GitSyncParameters? Create(ILogger? logger, SupportToolsParameters supportToolsParameters,
        string projectName, EGitCol gitCol, string gitProjectName, bool useConsole)
    {
        var project = supportToolsParameters.GetProject(projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Git Project with name {projectName} does not exists", true);
            return null;
        }

        var gitProjectNames = project.GetGitProjectNames(gitCol);

        if (!gitProjectNames.Contains(gitProjectName))
        {
            StShared.WriteErrorLine($"Git Project with name {gitProjectName} does not exists", true, logger);
            return null;
        }

        if (!supportToolsParameters.Gits.ContainsKey(gitProjectName))
        {
            StShared.WriteErrorLine($"Git Project with name {gitProjectName} does not exists in project {projectName}",
                true, logger);
            return null;
        }

        var gitProjects = GitProjects.Create(logger, supportToolsParameters.GitProjects);

        var gitRepos = GitRepos.Create(logger, supportToolsParameters.Gits,
            project.SpaProjectFolderRelativePath(gitProjects), useConsole, false);

        if (!gitRepos.Gits.TryGetValue(gitProjectName, out var gitDataDom))
        {
            StShared.WriteErrorLine($"Git Project with name {gitProjectName} does not exists in gits list", true,
                logger);
            return null;
        }

        var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);

        if (gitsFolder != null)
            return new GitSyncParameters(gitDataDom, gitsFolder);

        StShared.WriteErrorLine("Gits folder not found", true);
        return null;
    }
}