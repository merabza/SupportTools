using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.SyncAllProjectsAllGits;

public sealed class SyncAllProjectsAllGitsCliMenuCommandV2 : CliMenuCommand
{
    public const string MenuCommandName = "Sync All Projects All Gits V2";

    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncAllProjectsAllGitsCliMenuCommandV2(ILogger logger, IParametersManager parametersManager) : this(logger,
        parametersManager, RemoteOnlyProjects.InputBoolFromConsole)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncAllProjectsAllGitsCliMenuCommandV2(ILogger logger, IParametersManager parametersManager,
        Func<string, bool, bool> inputBool) : base(MenuCommandName, EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //remote პროექტები მხოლოდ ცხადი თანხმობით იკლონება (D3)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        bool includeRemoteProjects = RemoteOnlyProjects.AskInclude(parameters.Projects, _inputBool);

        var syncOneProjectAllGitsToolAction = SyncMultipleProjectsGitsToolActionV2.Create(_logger, _parametersManager,
            null, null, true, null, includeRemoteProjects);
        return await syncOneProjectAllGitsToolAction.Run(cancellationToken);
    }
}
