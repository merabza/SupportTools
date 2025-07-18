using System;
using System.Net.Http;
using System.Threading;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class ProjectServerToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly ServerInfoModel _serverInfo;
    private readonly EProjectServerTools _tool;
    private IToolCommand? _toolCommand;

    public ProjectServerToolTaskCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        EProjectServerTools tool, string projectName, ServerInfoModel serverInfo,
        IParametersManager parametersManager) : base(tool.GetProjectServerToolName(), EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tool = tool;
        _projectName = projectName;
        _serverInfo = serverInfo;
        _parametersManager = parametersManager;
    }

    private IToolCommand? MemoCreateToolCommand()
    {
        return _toolCommand ??= ToolCommandFactory.CreateProjectServerToolCommand(_logger, _httpClientFactory, _tool,
            _parametersManager, _projectName, _serverInfo);
    }

    protected override string? GetActionDescription()
    {
        return MemoCreateToolCommand()?.Description;
    }

    protected override bool RunBody()
    {
        var toolCommand = MemoCreateToolCommand();
        if (toolCommand?.Par != null) return toolCommand.Run(CancellationToken.None).Result;

        Console.WriteLine("Parameters not loaded. Tool not started.");
        return false;
    }
}