using CliMenu;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;

namespace SupportTools.CliMenuCommands;

public sealed class ServerInfoSubMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;
    private readonly string _serverName;

    public ServerInfoSubMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        string serverName) : base(serverName)
    {
        _logger = logger;
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
        ServerInfoCruder serverInfoCruder = new(_logger, _parametersManager, _projectName);
        var serverSubMenuSet = serverInfoCruder.GetItemMenu(_serverName); //, $"Project => {_projectName} => ");

        return serverSubMenuSet;
    }
}