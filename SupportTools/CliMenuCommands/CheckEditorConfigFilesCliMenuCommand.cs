using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.ToolActions;
using SupportTools.Tools;

namespace SupportTools.CliMenuCommands;

public sealed class CheckEditorConfigFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckEditorConfigFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        CheckEditorConfigFilesToolAction.ActionName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var checkEditorConfigFilesToolAction = new CheckEditorConfigFilesToolAction(_logger, _parametersManager, true);
        return await checkEditorConfigFilesToolAction.Run(cancellationToken);
    }

    protected override string GetStatus()
    {
        MenuAction = EMenuAction.Reload;
        var wrongEditorConfigFilesListCreator = new WrongEditorConfigFilesListCreator(_logger, _parametersManager);
        return wrongEditorConfigFilesListCreator.Create().Count.ToString(CultureInfo.InvariantCulture);
    }
}
