using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

public sealed class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2 : CliMenuCommand
{
    public const string MenuCommandName = "Sync One Project All Gits With Scaffold Seeders V2";
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(ILogger logger,
        IParametersManager parametersManager, string projectName, bool askRunAction = true) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, null, askRunAction)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, null, _projectName, true);
        return await syncOneGroupAllProjectsGitsToolAction.Run(cancellationToken);
    }
}
