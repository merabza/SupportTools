using System.Threading;
using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibGitWork.CliMenuCommands;

//
public sealed class UpdateOutdatedPackagesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOutdatedPackagesCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
        "Update Outdated Packages", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var updateOutdatedPackagesToolAction =
            UpdateOutdatedPackagesToolAction.Create(_logger, _parametersManager, null, null, true);
        return updateOutdatedPackagesToolAction.Run(CancellationToken.None).Result;
    }
}