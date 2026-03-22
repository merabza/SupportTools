using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;

// ReSharper disable ConvertToPrimaryConstructor

namespace SupportTools.CliMenuCommands;

public sealed class ProjectServerToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly ServerInfoModel _serverInfo;
    private readonly ServiceProvider _serviceProvider;
    private readonly EProjectServerTools _tool;
    private IToolCommand? _toolCommand;

    public ProjectServerToolTaskCliMenuCommand(EProjectServerTools tool, string projectName, ServerInfoModel serverInfo,
        IParametersManager parametersManager, ServiceProvider serviceProvider) : base(tool.GetProjectServerToolName(),
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _tool = tool;
        _projectName = projectName;
        _serverInfo = serverInfo;
        _parametersManager = parametersManager;
        _serviceProvider = serviceProvider;
    }

    private async ValueTask<IToolCommand> MemoCreateToolCommand()
    {
        return _toolCommand ??= await ToolCommandFactory.CreateProjectServerToolCommand(_tool, _serviceProvider,
            _parametersManager, _projectName, _serverInfo);
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
