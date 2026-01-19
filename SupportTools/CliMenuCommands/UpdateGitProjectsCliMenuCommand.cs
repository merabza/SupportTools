using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateGitProjectsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateGitProjectsCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        UpdateGitProjectsToolAction.ActionName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var updateGitProjectsToolAction = new UpdateGitProjectsToolAction(_logger, _parametersManager, true);
        return updateGitProjectsToolAction.Run(CancellationToken.None).Result;
    }
}