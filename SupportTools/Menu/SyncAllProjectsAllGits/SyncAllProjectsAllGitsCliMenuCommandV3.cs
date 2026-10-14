using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.SyncAllProjectsAllGits;

public sealed class SyncAllProjectsAllGitsCliMenuCommandV3 : CliMenuCommand
{
    public const string MenuCommandName = "Sync All Projects All Gits V3";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<string, bool, bool> _inputBool;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    public SyncAllProjectsAllGitsCliMenuCommandV3(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : this(logger, httpClientFactory, parametersManager,
        RemoteOnlyProjects.InputBoolFromConsole)
    {
    }

    //კონსოლიდან შეყვანა პარამეტრადაა გამოტანილი, რომ ტესტებმა პასუხი თვითონ მიაწოდონ
    // ReSharper disable once ConvertToPrimaryConstructor
    internal SyncAllProjectsAllGitsCliMenuCommandV3(ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager, Func<string, bool, bool> inputBool) : base(MenuCommandName,
        EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _inputBool = inputBool;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //remote პროექტები მხოლოდ ცხადი თანხმობით იკლონება (D3)
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        bool includeRemoteProjects = RemoteOnlyProjects.AskInclude(parameters.Projects, _inputBool);

        var syncMultipleProjectsGitsToolAction = SyncMultipleProjectsGitsToolActionV3.Create(_logger,
            _httpClientFactory, _parametersManager, null, null, includeRemoteProjects);
        return await syncMultipleProjectsGitsToolAction.Run(cancellationToken);
    }
}
