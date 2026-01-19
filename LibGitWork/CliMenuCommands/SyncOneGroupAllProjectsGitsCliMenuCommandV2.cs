using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncOneGroupAllProjectsGitsCliMenuCommandV2 : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneGroupAllProjectsGitsCliMenuCommandV2(ILogger logger, ParametersManager parametersManager,
        string projectGroupName) : base("Sync One Group All Projects Gits V2", EMenuAction.Reload, EMenuAction.Reload,
        null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
    }

    protected override bool RunBody()
    {
        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, _projectGroupName, null, true);
        return syncOneGroupAllProjectsGitsToolAction.Run(CancellationToken.None).Result;
    }
}