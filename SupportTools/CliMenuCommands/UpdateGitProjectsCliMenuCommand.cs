﻿using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateGitProjectsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitProjectsCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        UpdateGitProjectsToolAction.ActionName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        var updateGitProjectsToolAction = new UpdateGitProjectsToolAction(_logger, _parametersManager);
        updateGitProjectsToolAction.Run(CancellationToken.None).Wait();
    }
}