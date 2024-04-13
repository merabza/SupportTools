﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SupportToolsData;
using SystemToolsShared;
using System.Collections.Generic;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibGitWork.ToolActions;

public sealed class SyncOneProjectAllGitsToolAction : ToolAction
{
    private readonly SyncOneProjectAllGitsParameters _syncOneProjectAllGitsParameters;

    public SyncOneProjectAllGitsToolAction(ILogger logger,
        SyncOneProjectAllGitsParameters syncOneProjectAllGitsParameters) : base(
        logger,
        "Sync One Project All Gits", null, null)
    {
        _syncOneProjectAllGitsParameters = syncOneProjectAllGitsParameters;
    }

    public static SyncOneProjectAllGitsToolAction? Create(ILogger logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol,
        Dictionary<EGitCollect, Dictionary<string, List<string>>>? changedGitProjects, bool isFirstSync)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var gitSyncAllParameters = SyncOneProjectAllGitsParameters.Create(logger, supportToolsParameters, projectName,
            gitCol, changedGitProjects, isFirstSync);

        if (gitSyncAllParameters is not null)
            return new SyncOneProjectAllGitsToolAction(logger, gitSyncAllParameters);

        StShared.WriteErrorLine("SyncOneProjectAllGitsParameters is not created", true);
        return null;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        string? commitMessage = null;
        var changedGitProjects = _syncOneProjectAllGitsParameters.ChangedGitProjects;
        var projectName = _syncOneProjectAllGitsParameters.ProjectName;
        foreach (var gitData in _syncOneProjectAllGitsParameters.GitData.OrderBy(x => x.GitProjectFolderName))
        {
            var gitProjectFolderName = gitData.GitProjectFolderName;
            if (projectName is not null && !_syncOneProjectAllGitsParameters.IsFirstSync &&
                changedGitProjects is not null &&
                (!changedGitProjects[EGitCollect.Usage].TryGetValue(gitProjectFolderName, out var proListVal) ||
                 proListVal.Count == 1 && proListVal[0] == projectName))
                continue;
            var gitSync = new GitSyncToolAction(Logger,
                new GitSyncParameters(gitData, _syncOneProjectAllGitsParameters.GitsFolder),
                commitMessage, commitMessage == null);
            await gitSync.Run(cancellationToken);
            commitMessage = gitSync.UsedCommitMessage;
            if (projectName is null || changedGitProjects is null || !gitSync.Changed)
                continue;
            if (changedGitProjects[EGitCollect.Collect].TryGetValue(gitProjectFolderName, out var proList) &&
                !proList.Contains(projectName))
                proList.Add(projectName);
            changedGitProjects[EGitCollect.Collect].Add(gitProjectFolderName, [projectName]);
        }

        return true;
    }

}