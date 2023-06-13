using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using SupportToolsData.Domain;
using SupportToolsData.Models;
using SystemToolsShared;

namespace LibAppProjectCreator.Models;

public sealed class GitRepos
{
    private GitRepos(Dictionary<string, GitDataDomain> gitRepos)
    {
        Gits = gitRepos;
    }

    public Dictionary<string, GitDataDomain> Gits { get; }


    public static GitRepos Create(ILogger logger, Dictionary<string, GitDataModel> gitRepos,
        string? mainProjectFolderRelativePath, string? spaProjectFolderRelativePath)
    {
        Dictionary<string, GitDataDomain> gits = new();
        foreach (var kvp in gitRepos)
        {
            if (string.IsNullOrWhiteSpace(kvp.Value.GitProjectAddress))
            {
                logger.LogError("GitProjectAddress is empty for Git Repo with key {kvp.Key})", kvp.Key);
                continue;
            }

            if (string.IsNullOrWhiteSpace(kvp.Value.GitProjectFolderName))
            {
                logger.LogError("GitProjectFolderName is empty for Git Repo with key {kvp.Key})", kvp.Key);
                continue;
            }

            var gitProjectFolderName = kvp.Value.GitProjectFolderName;
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

            gits.Add(kvp.Key, new GitDataDomain(kvp.Value.GitProjectAddress, gitProjectFolderName));
        }

        return new GitRepos(gits);
    }

    public GitDataDomain? GetGitRepoByKey(string key)
    {
        return Gits.ContainsKey(key) ? Gits[key] : null;
    }
}