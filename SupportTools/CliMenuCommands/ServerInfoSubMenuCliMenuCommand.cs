using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using System.Net.Http;

namespace SupportTools.CliMenuCommands;

public sealed class ServerInfoSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;
    private readonly string _serverName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInfoSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectName, string serverName) : base(serverName)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _serverName = serverName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }


    public override CliMenuSet GetSubmenu()
    {
        ServerInfoCruder serverInfoCruder = new(_logger, _httpClientFactory, _parametersManager, _projectName);
        var serverSubMenuSet = serverInfoCruder.GetItemMenu(_serverName); //, $"Project => {_projectName} => ");

        return serverSubMenuSet;
    }
}