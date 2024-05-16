using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateGitIgnoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitIgnoreFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        UpdateGitIgnoreFilesToolAction.ActionName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
        var updateGitProjectsToolAction = new UpdateGitIgnoreFilesToolAction(_logger, _parametersManager);
        updateGitProjectsToolAction.Run(CancellationToken.None).Wait();
    }
}