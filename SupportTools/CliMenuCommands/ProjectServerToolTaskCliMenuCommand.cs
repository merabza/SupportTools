using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
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

    private async ValueTask<IToolCommand> MemoCreateToolCommand()
    {
        return _toolCommand ??= await ToolCommandFactory.CreateProjectServerToolCommand(_logger, _httpClientFactory,
            _tool, _parametersManager, _projectName, _serverInfo);
    }

    protected override async ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return (await MemoCreateToolCommand()).Description;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        IToolCommand toolCommand = await MemoCreateToolCommand();
        return await toolCommand.Run(cancellationToken);
    }
}
