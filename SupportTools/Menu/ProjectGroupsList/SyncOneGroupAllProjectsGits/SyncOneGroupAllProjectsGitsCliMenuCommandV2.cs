using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

public sealed class SyncOneGroupAllProjectsGitsCliMenuCommandV2 : CliMenuCommand
{
    public const string MenuCommandName = "Sync One Group All Projects Gits V2";

    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;

    public SyncOneGroupAllProjectsGitsCliMenuCommandV2(ILogger logger, IParametersManager parametersManager,
        string projectGroupName) : this(logger, parametersManager, projectGroupName,
        RemoteOnlyProjects.InputBoolFromConsole)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncOneGroupAllProjectsGitsCliMenuCommandV2(ILogger logger, IParametersManager parametersManager,
        string projectGroupName, Func<string, bool, bool> inputBool) : base(MenuCommandName, EMenuAction.Reload,
        EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //remote პროექტები მხოლოდ ცხადი თანხმობით იკლონება (D3)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        bool includeRemoteProjects = RemoteOnlyProjects.AskInclude(
            parameters.Projects.Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName), _inputBool);

        var syncOneGroupAllProjectsGitsToolAction = SyncMultipleProjectsGitsToolActionV2.Create(_logger,
            _parametersManager, _projectGroupName, null, true, null, includeRemoteProjects);
        return await syncOneGroupAllProjectsGitsToolAction.Run(cancellationToken);
    }
}
