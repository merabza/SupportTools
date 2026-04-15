using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.UpdateOutdatedPackages;

public sealed class UpdateOutdatedPackagesCliMenuCommand : CliMenuCommand
{
    public const string MenuCommandName = "Update Outdated Packages";

    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateOutdatedPackagesCliMenuCommand(ILogger logger, ParametersManager parametersManager) : base(
        "Update Outdated Packages", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var updateOutdatedPackagesToolAction =
            UpdateOutdatedPackagesToolAction.Create(_logger, _parametersManager, null, null, true);
        return await updateOutdatedPackagesToolAction.Run(cancellationToken);
    }
}
