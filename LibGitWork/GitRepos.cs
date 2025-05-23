﻿using System.Collections.Generic;
using System.IO;
using LibGitData.Models;
using Microsoft.Extensions.Logging;
using SupportToolsServerApiContracts.Models;
using SystemToolsShared;

namespace LibGitWork;

public sealed class GitRepos
{
    private GitRepos(Dictionary<string, GitDataDto> gitRepos)
    {
        Gits = gitRepos;
    }

    public Dictionary<string, GitDataDto> Gits { get; }

    public static GitRepos Create(ILogger? logger, Dictionary<string, GitDataModel> gitRepos,
        string? spaProjectFolderRelativePath, bool useConsole, bool useGitRecordNameForComplexGitProjectFolderName)
    {
        Dictionary<string, GitDataDto> gits = [];
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

            if (gitProjectFolderName.StartsWith(GitDataModel.SpaProjectFolderRelativePathName))
            {
                if (useGitRecordNameForComplexGitProjectFolderName)
                {
                    gitProjectFolderName = gitProjectName;
                }
                else
                {
                    if (spaProjectFolderRelativePath is null)
                        continue;
                    gitProjectFolderName = Path.Combine(spaProjectFolderRelativePath,
                        gitProjectFolderName[GitDataModel.SpaProjectFolderRelativePathName.Length..]
                            .RemoveNotNeedLeadPart(Path.DirectorySeparatorChar));
                }
            }

            gits.Add(gitProjectName,
                new GitDataDto
                {
                    GitProjectAddress = gitData.GitProjectAddress,
                    GitProjectFolderName = gitProjectFolderName,
                    GitProjectName = gitProjectFolderName,
                    GitIgnorePathName = gitData.GitIgnorePathName
                });
        }

        return new GitRepos(gits);
    }

    public GitDataDto? GetGitRepoByKey(string key)
    {
        return Gits.GetValueOrDefault(key);
    }
}