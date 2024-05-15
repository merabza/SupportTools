using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand(ILogger logger, ParametersManager parametersManager,
        string projectName,
        bool askRunAction = true) : base("Sync One Project All Gits With Scaffold Seeders", null, askRunAction)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolAction.Create(_logger, _parametersManager, null, _projectName);
        syncOneGroupAllProjectsGitsToolAction.Run(CancellationToken.None).Wait();
    }
}