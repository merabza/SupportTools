using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncOneProjectAllGitsCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        EGitCol gitCol, bool askRunAction = true) : base("Sync One Project All gits", null, askRunAction)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitCol = gitCol;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        var syncOneProjectAllGitsToolAction =
            SyncOneProjectAllGitsToolAction.Create(_logger, _parametersManager, _projectName, _gitCol, null, true);
        syncOneProjectAllGitsToolAction?.Run(CancellationToken.None).Wait();
        //StShared.Pause();

    }
}