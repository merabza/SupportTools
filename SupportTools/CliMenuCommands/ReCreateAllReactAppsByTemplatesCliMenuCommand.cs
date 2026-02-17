using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibAppProjectCreator.ToolCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateAllReactAppsByTemplatesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public ReCreateAllReactAppsByTemplatesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        "ReCreate All React Apps By Templates", EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var reCreateAllReactAppsByTemplatesToolCommand =
            new ReCreateAllReactAppsByTemplatesToolCommand(_logger, Name, _parametersManager.Parameters,
                _parametersManager);
        return await reCreateAllReactAppsByTemplatesToolCommand.Run(cancellationToken);
    }
}
