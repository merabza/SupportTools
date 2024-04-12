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

    public SyncOneProjectAllGitsToolAction(ILogger logger, SyncOneProjectAllGitsParameters syncOneProjectAllGitsParameters) : base(
        logger,
        "Sync One Project All Gits", null, null)
    {
        _syncOneProjectAllGitsParameters = syncOneProjectAllGitsParameters;
    }

    public static SyncOneProjectAllGitsToolAction? Create(ILogger logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol, List<string>? changedGitProjects, EGitCollect? gitCollect)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var gitSyncAllParameters = SyncOneProjectAllGitsParameters.Create(logger, supportToolsParameters, projectName,
            gitCol, changedGitProjects, gitCollect);


        if (gitSyncAllParameters is not null)
            return new SyncOneProjectAllGitsToolAction(logger, gitSyncAllParameters);

        StShared.WriteErrorLine("SyncOneProjectAllGitsParameters is not created", true);
        return null;

    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        string? commitMessage = null;
        foreach (var gitData in _syncOneProjectAllGitsParameters.GitData.OrderBy(x => x.GitProjectFolderName))
        {
            if (_syncOneProjectAllGitsParameters is { GitCollect: EGitCollect.Usage, ChangedGitProjects: not null } &&
                !_syncOneProjectAllGitsParameters.ChangedGitProjects.Contains(gitData.GitProjectFolderName))
                continue;
            var gitSync = new GitSyncToolAction(Logger, new GitSyncParameters(gitData, _syncOneProjectAllGitsParameters.GitsFolder),
                commitMessage, commitMessage == null);
            await gitSync.Run(cancellationToken);
            commitMessage = gitSync.UsedCommitMessage;
            if ( gitSync.Changed)
                _syncOneProjectAllGitsParameters.ChangedGitProjects?.Add(gitData.GitProjectFolderName);
        }

        return true;
    }

}