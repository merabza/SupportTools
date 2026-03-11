using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibTools.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibTools.CliMenuCommands;

public sealed class ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(ILogger logger, ParametersManager parametersManager) :
        base("Clear All Groups All Solutions All Projects", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var clearAllProjectsToolAction =
            ClearMultipleProjectsToolAction.Create(_logger, _parametersManager, null, null, true);
        return await clearAllProjectsToolAction.Run(cancellationToken);
    }
}
