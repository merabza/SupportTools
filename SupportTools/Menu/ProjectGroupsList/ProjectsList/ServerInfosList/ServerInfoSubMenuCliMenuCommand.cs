using System;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ServerInfosList;

public sealed class ServerInfoSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    private readonly string _projectName;
    private readonly string _serverName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInfoSubMenuCliMenuCommand(IApplication application, ILogger logger,
        IHttpClientFactory httpClientFactory, string itemKey, IParametersManager parametersManager, string projectName,
        string serverName, IServiceProvider serviceProvider) : base(itemKey, EMenuAction.LoadSubMenu)
    {
        _application = application;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverName = serverName;
        _serviceProvider = serviceProvider;
    }

    public override CliMenuSet GetSubMenu()
    {
        var serverInfoCruder = ServerInfoCruder.Create(_application, _serviceProvider, _logger, _httpClientFactory,
            _parametersManager, _projectName);
        CliMenuSet serverSubMenuSet = serverInfoCruder.GetItemMenu(_serverName); //, $"Project => {_projectName} => ");

        return serverSubMenuSet;
    }
}
