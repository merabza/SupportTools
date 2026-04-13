using System;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.CliMenuCommands;

public sealed class ServerInfoSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    private readonly string _projectName;
    private readonly string _serverName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInfoSubMenuCliMenuCommand(string appName, ILogger logger, IHttpClientFactory httpClientFactory,
        string itemKey, IParametersManager parametersManager, string projectName, string serverName,
        IServiceProvider serviceProvider) : base(itemKey, EMenuAction.LoadSubMenu)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverName = serverName;
        _serviceProvider = serviceProvider;
    }

    public override CliMenuSet GetSubMenu()
    {
        var serverInfoCruder = ServerInfoCruder.Create(_appName, _serviceProvider, _logger, _httpClientFactory,
            _parametersManager, _projectName);
        CliMenuSet serverSubMenuSet = serverInfoCruder.GetItemMenu(_serverName); //, $"Project => {_projectName} => ");

        return serverSubMenuSet;
    }
}
