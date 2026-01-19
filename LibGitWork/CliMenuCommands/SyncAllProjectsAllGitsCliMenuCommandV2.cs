using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibGitWork.CliMenuCommands;

public sealed class SyncAllProjectsAllGitsCliMenuCommandV2 : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncAllProjectsAllGitsCliMenuCommandV2(ILogger logger, ParametersManager parametersManager) : base(
        "Sync All Projects All Gits V2", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var syncOneProjectAllGitsToolAction =
            SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager, null, null, true);
        return syncOneProjectAllGitsToolAction.Run(CancellationToken.None).Result;
    }
}