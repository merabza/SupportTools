﻿using System;
using System.Net.Http;
using System.Threading;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using SupportToolsData.Models;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class ToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly ServerInfoModel? _serverInfo;
    private readonly ETools _tool;
    private IToolCommand? _toolCommand;

    public ToolTaskCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, ETools tool, string projectName,
        ServerInfoModel? serverInfo, IParametersManager parametersManager) : base(null, EMenuAction.Reload,
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
        return _toolCommand ??= _serverInfo is null
            ? ToolCommandFabric.Create(_logger, _tool, _parametersManager, _projectName, true)
            : ToolCommandFabric.Create(_logger, _httpClientFactory, _tool, _parametersManager, _projectName,
                _serverInfo);
    }

    protected override string? GetActionDescription()
    {
        return MemoCreateToolCommand()?.Description;
    }

    protected override bool RunBody()
    {
        var toolCommand = MemoCreateToolCommand();
        if (toolCommand?.Par == null)
        {
            Console.WriteLine("Parameters not loaded. Tool not started.");
            return false;
        }

        toolCommand.Run(CancellationToken.None).Wait();
        return true;
    }
}