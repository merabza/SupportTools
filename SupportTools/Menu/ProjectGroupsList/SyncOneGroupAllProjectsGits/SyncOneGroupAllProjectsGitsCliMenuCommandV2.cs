using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

public sealed class SyncOneGroupAllProjectsGitsCliMenuCommandV2 : CliMenuCommand
{
    public const string MenuCommandName = "Sync One Group All Projects Gits V2";

    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneGroupAllProjectsGitsCliMenuCommandV2(ILogger logger, IParametersManager parametersManager,
        string projectGroupName) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
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
