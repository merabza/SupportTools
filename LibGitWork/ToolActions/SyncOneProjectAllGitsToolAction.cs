using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGitData;
using LibGitWork.ToolCommandParameters;
using LibParameters;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;
using SystemToolsShared;

// ReSharper disable ConvertToPrimaryConstructor

namespace LibGitWork.ToolActions;

public sealed class SyncOneProjectAllGitsToolAction : ToolAction
{
    private readonly ILogger? _logger;
    private readonly SyncOneProjectAllGitsParameters _syncOneProjectAllGitsParameters;

    public SyncOneProjectAllGitsToolAction(ILogger? logger,
        SyncOneProjectAllGitsParameters syncOneProjectAllGitsParameters) : base(logger, "Sync One Project All Gits",
        null, null)
    {
        _logger = logger;
        _syncOneProjectAllGitsParameters = syncOneProjectAllGitsParameters;
    }

    public static SyncOneProjectAllGitsToolAction? Create(ILogger? logger, ParametersManager parametersManager,
        string projectName, EGitCol gitCol,
        Dictionary<EGitCollect, Dictionary<string, List<string>>>? changedGitProjects, bool isFirstSync,
        bool useConsole)
    {
        var supportToolsParameters = (SupportToolsParameters)parametersManager.Parameters;
        var loggerOrNull = supportToolsParameters.LogGitWork ? logger : null;
        var syncOneProjectAllGitsParameters = SyncOneProjectAllGitsParameters.Create(loggerOrNull,
            supportToolsParameters, projectName, gitCol, changedGitProjects, isFirstSync, useConsole);

        if (syncOneProjectAllGitsParameters is not null)
            return new SyncOneProjectAllGitsToolAction(loggerOrNull, syncOneProjectAllGitsParameters);

        StShared.WriteErrorLine("SyncOneProjectAllGitsParameters is not created", true);
        return null;
    }

    protected override async Task<bool> RunAction(CancellationToken cancellationToken = default)
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
                 (proListVal.Count == 1 && proListVal[0] == projectName)))
                continue;
            var gitSync = new GitSyncToolAction(_logger,
                new GitSyncParameters(gitData, _syncOneProjectAllGitsParameters.GitsFolder), commitMessage,
                commitMessage == null);
            await gitSync.Run(cancellationToken);
            commitMessage = gitSync.UsedCommitMessage;
            if (projectName is null || changedGitProjects is null || !gitSync.Changed)
                continue;
            if (changedGitProjects[EGitCollect.Collect].TryGetValue(gitProjectFolderName, out var proList))
            {
                if (!proList.Contains(projectName))
                    proList.Add(projectName);
            }
            else
            {
                changedGitProjects[EGitCollect.Collect].Add(gitProjectFolderName, [projectName]);
            }
        }

        return true;
    }
}