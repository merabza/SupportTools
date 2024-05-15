﻿using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncAllProjectsAllGitsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncAllProjectsAllGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
        "Sync All Projects All Gits", null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        var syncOneProjectAllGitsToolAction =
            SyncMultipleProjectsGitsToolAction.Create(_logger, _parametersManager, null, null);
        syncOneProjectAllGitsToolAction.Run(CancellationToken.None).Wait();
    }
}