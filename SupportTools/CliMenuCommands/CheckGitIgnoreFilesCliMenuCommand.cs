﻿using System.Threading;
using CliMenu;
using LibGitWork;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.CliMenuCommands;

public sealed class CheckGitIgnoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGitIgnoreFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        CheckGitIgnoreFilesToolAction.ActionName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        var updateGitProjectsToolAction = new CheckGitIgnoreFilesToolAction(_logger, _parametersManager);
        updateGitProjectsToolAction.Run(CancellationToken.None).Wait();
    }

    protected override string GetStatus()
    {
        MenuAction = EMenuAction.Reload;
        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(_logger, _parametersManager);
        var wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        return wrongGitIgnoreFilesList.Count.ToString();
    }
}