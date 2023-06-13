using System;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IToolCommand? _toolCommand;

    public ToolTaskCliMenuCommand(ILogger logger, ETools tool, string projectName, string? serverName,
        IParametersManager parametersManager) : base(null, null, true)
    {
        _toolCommand = string.IsNullOrWhiteSpace(serverName)
            ? ToolCommandFabric.Create(logger, tool, parametersManager, projectName)
            : ToolCommandFabric.Create(logger, tool, parametersManager, projectName, serverName);
    }

    protected override string? GetActionDescription()
    {
        return _toolCommand?.Description;
    }

    protected override void RunAction()
    {
        if (_toolCommand?.Par == null)
        {
            Console.WriteLine("Parameters not loaded. Tool not started.");
            StShared.Pause();
            return;
        }

        //დავინიშნოთ დრო
        var startDateTime = DateTime.Now;

        Console.WriteLine("Tools is running...");
        Console.WriteLine("---");

        _toolCommand.Run();

        Console.WriteLine("---");

        Console.WriteLine($"Tool Finished. {StShared.TimeTakenMessage(startDateTime)}");


        StShared.Pause();
        MenuAction = EMenuAction.Reload;
    }
}