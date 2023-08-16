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
    private readonly ILogger _logger;
    private readonly ETools _tool;
    private readonly string _projectName;
    private readonly ServerInfoModel? _serverInfo;
    private readonly IParametersManager _parametersManager;
    private IToolCommand? _toolCommand;

    public ToolTaskCliMenuCommand(ILogger logger, ETools tool, string projectName, ServerInfoModel? serverInfo,
        IParametersManager parametersManager) : base(null, null, true)
    {
        _logger = logger;
        _tool = tool;
        _projectName = projectName;
        _serverInfo = serverInfo;
        _parametersManager = parametersManager;
    }

    private IToolCommand? MemoCreateToolCommand()
    {
        return _toolCommand ??= _serverInfo is null
            ? ToolCommandFabric.Create(_logger, _tool, _parametersManager, _projectName)
            : ToolCommandFabric.Create(_logger, _tool, _parametersManager, _projectName, _serverInfo);
    }

    protected override string? GetActionDescription()
    {
        return MemoCreateToolCommand()?.Description;
    }

    protected override void RunAction()
    {
        var toolCommand = MemoCreateToolCommand();
        if (toolCommand?.Par == null)
        {
            Console.WriteLine("Parameters not loaded. Tool not started.");
            StShared.Pause();
            return;
        }

        //დავინიშნოთ დრო
        var startDateTime = DateTime.Now;

        Console.WriteLine("Tools is running...");
        Console.WriteLine("---");

        toolCommand.Run();

        Console.WriteLine("---");

        Console.WriteLine($"Tool Finished. {StShared.TimeTakenMessage(startDateTime)}");


        StShared.Pause();
        MenuAction = EMenuAction.Reload;
    }
}