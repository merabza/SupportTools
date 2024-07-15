using System.Collections.Generic;
using System.IO;
using LibGitData.Domain;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork;

public sealed class GitRepos
{
    private GitRepos(Dictionary<string, GitDataDomain> gitRepos)
    {
        Gits = gitRepos;
    }

    public Dictionary<string, GitDataDomain> Gits { get; }


    public static GitRepos Create(ILogger? logger, Dictionary<string, GitDataModel> gitRepos,
        string? mainProjectFolderRelativePath, string? spaProjectFolderRelativePath, bool useConsole)
    {
        Dictionary<string, GitDataDomain> gits = [];
        foreach (var (gitProjectName, gitData) in gitRepos)
        {
            if (string.IsNullOrWhiteSpace(gitData.GitProjectAddress))
            {
                StShared.WriteErrorLine($"GitProjectAddress is empty for Git Repo with key {gitProjectName})",
                    useConsole, logger);
                continue;
            }

            if (string.IsNullOrWhiteSpace(gitData.GitProjectFolderName))
            {
                StShared.WriteErrorLine($"GitProjectFolderName is empty for Git Repo with key {gitProjectName})",
                    useConsole, logger);
                continue;
            }

            if (string.IsNullOrWhiteSpace(gitData.GitIgnorePathName))
            {
                StShared.WriteErrorLine($"GitIgnorePathName is empty for Git Repo with key {gitProjectName})",
                    useConsole, logger);
                continue;
            }

            var gitProjectFolderName = gitData.GitProjectFolderName;
            if (gitProjectFolderName.StartsWith(GitDataModel.MainProjectFolderRelativePathName))
            {
                if (mainProjectFolderRelativePath is null)
                    continue;
                gitProjectFolderName = Path.Combine(mainProjectFolderRelativePath,
                    gitProjectFolderName[GitDataModel.MainProjectFolderRelativePathName.Length..]
                        .RemoveNotNeedLeadPart(Path.DirectorySeparatorChar));
            }

            if (gitProjectFolderName.StartsWith(GitDataModel.SpaProjectFolderRelativePathName))
            {
                if (spaProjectFolderRelativePath is null)
                    continue;
                gitProjectFolderName = Path.Combine(spaProjectFolderRelativePath,
                    gitProjectFolderName[GitDataModel.SpaProjectFolderRelativePathName.Length..]
                        .RemoveNotNeedLeadPart(Path.DirectorySeparatorChar));
            }

            gits.Add(gitProjectName,
                new GitDataDomain(gitData.GitProjectAddress, gitProjectFolderName, gitData.GitIgnorePathName));
        }

        return new GitRepos(gits);
    }

    public GitDataDomain? GetGitRepoByKey(string key)
    {
        return Gits.GetValueOrDefault(key);
    }
}