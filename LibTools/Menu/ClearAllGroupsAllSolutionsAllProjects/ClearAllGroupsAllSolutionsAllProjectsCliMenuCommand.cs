using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibTools.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibTools.Menu.ClearAllGroupsAllSolutionsAllProjects;

public sealed class ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Clear All Groups All Solutions All Projects";

    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(ILogger logger, IParametersManager parametersManager) :
        base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
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
