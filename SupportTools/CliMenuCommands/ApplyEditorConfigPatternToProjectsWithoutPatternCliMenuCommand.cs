using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.ToolActions;

namespace SupportTools.CliMenuCommands;

public sealed class ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand : CliMenuCommand
{
    private readonly string _editorConfigPatternName;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplyEditorConfigPatternToProjectsWithoutPatternCliMenuCommand(ILogger logger,
        string editorConfigPatternName, IParametersManager parametersManager) : base(
        "Apply this pattern to all projects that do not have an .editorconfig pattern specified",
        EMenuAction.Reload)
    {
        _logger = logger;
        _editorConfigPatternName = editorConfigPatternName;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var applyEditorConfigPatternToolAction =
            new ApplyEditorConfigPatternToProjectsWithoutPatternToolAction(_logger, _editorConfigPatternName,
                _parametersManager);
        return await applyEditorConfigPatternToolAction.Run(cancellationToken);
    }
}
