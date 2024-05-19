using CliMenu;
using CliParametersDataEdit.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using System.Threading;
using SupportTools.ToolActions;

namespace SupportTools.CliMenuCommands;

public sealed class ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand : CliMenuCommand
{
    private readonly string _gitIgnoreFileName;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedCliMenuCommand(ILogger logger,
        string gitIgnoreFileName, IParametersManager parametersManager) : base(null, EMenuAction.Reload)
    {
        _logger = logger;
        _gitIgnoreFileName = gitIgnoreFileName;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var getDbServerFoldersToolAction =
            new ApplyThisFileTypeToAllProjectsThatDoNotHaveATypeSpecifiedToolAction(_logger, _gitIgnoreFileName,
                _parametersManager);
        return getDbServerFoldersToolAction.Run(CancellationToken.None).Result;
    }
}