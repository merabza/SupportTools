using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
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

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, _projectGroupName, null, true);
        return await syncOneGroupAllProjectsGitsToolAction.Run(cancellationToken);
    }
}
