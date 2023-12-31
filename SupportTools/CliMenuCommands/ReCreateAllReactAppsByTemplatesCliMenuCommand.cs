using System;
using System.Threading;
using CliMenu;
using LibAppProjectCreator.ToolCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;
// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class ReCreateAllReactAppsByTemplatesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public ReCreateAllReactAppsByTemplatesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        "ReCreate All React Apps By Templates")
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override string GetActionDescription()
    {
        return Name!;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        try
        {
            ReCreateAllReactAppsByTemplatesToolCommand reCreateAllReactAppsByTemplatesToolCommand =
                new(_logger, Name!, _parametersManager.Parameters, _parametersManager);
            reCreateAllReactAppsByTemplatesToolCommand.Run(CancellationToken.None).Wait();

            StShared.Pause();

            return;
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }

        MenuAction = EMenuAction.Reload;
    }
}