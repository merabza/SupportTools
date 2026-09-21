using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.ToolActions;

namespace SupportTools.CliMenuCommands;

public sealed class UpdateEditorConfigFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateEditorConfigFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        UpdateEditorConfigFilesToolAction.ActionName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var updateEditorConfigFilesToolAction =
            new UpdateEditorConfigFilesToolAction(_logger, _parametersManager, true);
        return await updateEditorConfigFilesToolAction.Run(cancellationToken);
    }
}
