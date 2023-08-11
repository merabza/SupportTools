using System;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;
using SystemToolsShared;

namespace SupportTools.CliMenuCommands;

public sealed class ToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IToolCommand? _toolCommand;

    public ToolTaskCliMenuCommand(ILogger logger, ETools tool, string projectName, ServerInfoModel? serverInfo,
        IParametersManager parametersManager) : base(null, null, true)
    {
        _toolCommand = serverInfo is null
            ? ToolCommandFabric.Create(logger, tool, parametersManager, projectName)
            : ToolCommandFabric.Create(logger, tool, parametersManager, projectName, serverInfo);
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