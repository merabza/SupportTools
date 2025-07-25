﻿using System.Threading;
using CliMenu;
using LibGitData;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibGitWork.CliMenuCommands;

public sealed class GitSyncCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSyncCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        string gitProjectName, EGitCol gitCol) : base("Sync", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    protected override string GetActionDescription()
    {
        return $"This process will Sync git {_gitProjectName}";
    }

    protected override bool RunBody()
    {
        var gitSyncToolAction =
            GitSyncToolAction.Create(_logger, _parametersManager, _projectName, _gitCol, _gitProjectName, true);
        return gitSyncToolAction?.Run(CancellationToken.None).Result ?? false;
    }
}