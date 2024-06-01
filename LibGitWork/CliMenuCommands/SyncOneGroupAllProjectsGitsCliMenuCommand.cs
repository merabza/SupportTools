using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncOneGroupAllProjectsGitsCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneGroupAllProjectsGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager,
        string projectGroupName) : base("Sync One Group All Projects Gits", EMenuAction.Reload, EMenuAction.Reload,
        null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
    }

    protected override bool RunBody()
    {
        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolAction.Create(_logger, _parametersManager, _projectGroupName, null);
        return syncOneGroupAllProjectsGitsToolAction.Run(CancellationToken.None).Result;
    }
}