using LibParameters;
using SupportToolsData;
using SupportToolsData.Domain;
using SupportToolsData.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork.ToolCommandParameters;

public class GitSyncParameters : IParameters
{
    public GitDataDomain GitData { get; }
    public string GitsFolder { get; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSyncParameters(GitDataDomain gitData, string gitsFolder)
    {
        GitData = gitData;
        GitsFolder = gitsFolder;
    }


    public bool CheckBeforeSave()
    {
        return true;
    }

    public static GitSyncParameters? Create(ILogger logger, SupportToolsParameters supportToolsParameters,
        string projectName, EGitCol gitCol, string gitProjectName)
    {

        var project = supportToolsParameters.GetProject(projectName);
        if (project is null)
        {
            StShared.WriteErrorLine($"Git Project with name {projectName} does not exists", true);
            return null;
        }

        var result = supportToolsParameters.GetGitProjectNames(projectName, gitCol);
        if (result.IsNone)
        {
            StShared.WriteErrorLine($"Git Project with name {projectName} does not exists", true);
            return null;
        }

        var gitProjectNames = (List<string>)result;

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
            project.MainProjectFolderRelativePath(gitProjects), project.SpaProjectFolderRelativePath(gitProjects));

        if (!gitRepos.Gits.TryGetValue(gitProjectName, out var value))
        {
            StShared.WriteErrorLine($"Git Project with name {gitProjectName} does not exists in gits list", true,
                logger);
            return null;
        }

        var gitsFolder = supportToolsParameters.GetGitsFolder(projectName, gitCol);

        if (gitsFolder != null)
            return new GitSyncParameters(value, gitsFolder);

        StShared.WriteErrorLine("Gits folder not found", true);
        return null;

    }
}