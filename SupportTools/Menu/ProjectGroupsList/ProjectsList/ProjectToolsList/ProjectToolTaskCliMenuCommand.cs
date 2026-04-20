using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectToolsList;

public sealed class ProjectToolTaskCliMenuCommand : CliMenuCommand
{
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly IServiceProvider _serviceProvider;
    private readonly EProjectTools _tool;
    private IToolCommand? _toolCommand;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectToolTaskCliMenuCommand(IServiceProvider serviceProvider, EProjectTools tool, string projectName,
        IParametersManager parametersManager) : base(tool.GetProjectToolName(), EMenuAction.Reload, EMenuAction.Reload,
        null, true)
    {
        _serviceProvider = serviceProvider;
        _tool = tool;
        _projectName = projectName;
        _parametersManager = parametersManager;
    }

    private async ValueTask<IToolCommand?> MemoCreateToolCommand()
    {
        return _toolCommand ??=
            await ToolCommandFactory.CreateProjectToolCommand(_tool, _serviceProvider, _parametersManager,
                _projectName);
    }

    protected override async ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        IToolCommand? toolCommand = await MemoCreateToolCommand();
        return toolCommand?.Description;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        IToolCommand? toolCommand = await MemoCreateToolCommand();
        if (toolCommand?.Par != null)
        {
            return await toolCommand.Run(cancellationToken);
        }

        Console.WriteLine("Parameters not loaded. Tool not started.");
        return false;
    }
}
