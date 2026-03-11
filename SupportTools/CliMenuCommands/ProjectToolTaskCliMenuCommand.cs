using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class ProjectToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly EProjectTools _tool;
    private IToolCommand? _toolCommand;

    public ProjectToolTaskCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, EProjectTools tool,
        string projectName, IParametersManager parametersManager) : base(tool.GetProjectToolName(), EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tool = tool;
        _projectName = projectName;
        _parametersManager = parametersManager;
    }

    private IToolCommand? MemoCreateToolCommand()
    {
        return _toolCommand ??= ToolCommandFactory.CreateProjectToolCommand(_logger, _httpClientFactory, _tool,
            _parametersManager, _projectName, true);
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(MemoCreateToolCommand()?.Description);
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        IToolCommand? toolCommand = MemoCreateToolCommand();
        if (toolCommand?.Par != null)
        {
            return await toolCommand.Run(cancellationToken);
        }

        Console.WriteLine("Parameters not loaded. Tool not started.");
        return false;
    }
}
