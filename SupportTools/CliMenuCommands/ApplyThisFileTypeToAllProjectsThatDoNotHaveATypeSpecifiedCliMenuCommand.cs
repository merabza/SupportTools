using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.ToolActions;

namespace SupportTools.CliMenuCommands;

public sealed class ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand : CliMenuCommand
{
    private readonly string _gitIgnoreFileName;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand(ILogger logger,
        string gitIgnoreFileName, IParametersManager parametersManager) : base(
        "Apply this file type to all projects that do not have a type specified", EMenuAction.Reload)
    {
        _logger = logger;
        _gitIgnoreFileName = gitIgnoreFileName;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var getDbServerFoldersToolAction =
            new ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedToolAction(_logger, _gitIgnoreFileName,
                _parametersManager);
        return await getDbServerFoldersToolAction.Run(cancellationToken);
    }
}
