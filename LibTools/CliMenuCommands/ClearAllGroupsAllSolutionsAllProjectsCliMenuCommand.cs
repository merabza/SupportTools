using System.Threading;
using CliMenu;
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

    protected override bool RunBody()
    {
        var clearAllProjectsToolAction =
            ClearMultipleProjectsToolAction.Create(_logger, _parametersManager, null, null, true);
        return clearAllProjectsToolAction.Run(CancellationToken.None).Result;
    }
}