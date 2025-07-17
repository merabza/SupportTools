using System.Net.Http;
using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;

namespace SupportTools.CliMenuCommands;

public sealed class ServerInfoSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;
    private readonly string _serverName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInfoSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, string itemKey,
        ParametersManager parametersManager, string projectName, string serverName) : base(itemKey,
        EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverName = serverName;
    }

    public override CliMenuSet GetSubMenu()
    {
        var serverInfoCruder = ServerInfoCruder.Create(_logger, _httpClientFactory, _parametersManager, _projectName);
        var serverSubMenuSet = serverInfoCruder.GetItemMenu(_serverName); //, $"Project => {_projectName} => ");

        return serverSubMenuSet;
    }
}