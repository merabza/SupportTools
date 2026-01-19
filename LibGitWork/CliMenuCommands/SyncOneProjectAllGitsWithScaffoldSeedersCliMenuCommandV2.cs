using System.Threading;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2 : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(ILogger logger, ParametersManager parametersManager,
        string projectName, bool askRunAction = true) : base("Sync One Project All Gits With Scaffold Seeders V2",
        EMenuAction.Reload, EMenuAction.Reload, null, askRunAction)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override bool RunBody()
    {
        var syncOneGroupAllProjectsGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, null, _projectName, true);
        return syncOneGroupAllProjectsGitsToolAction.Run(CancellationToken.None).Result;
    }
}