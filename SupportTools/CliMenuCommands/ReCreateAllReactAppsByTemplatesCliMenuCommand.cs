using System.Threading;
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

    protected override bool RunBody()
    {
        var reCreateAllReactAppsByTemplatesToolCommand =
            new ReCreateAllReactAppsByTemplatesToolCommand(_logger, Name, _parametersManager.Parameters,
                _parametersManager);
        return reCreateAllReactAppsByTemplatesToolCommand.Run(CancellationToken.None).Result;
    }
}