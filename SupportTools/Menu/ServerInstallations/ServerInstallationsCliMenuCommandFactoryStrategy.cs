using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;

namespace SupportTools.Menu.ServerInstallations;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ServerInstallationsCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInstallationsCliMenuCommandFactoryStrategy(IParametersManager parametersManager)
    {
        _parametersManager = parametersManager;
    }

    //"Server Installations": New Server Installation და არსებული ინსტალაციების სია
    public CliMenuCommand CreateMenuCommand()
    {
        return new CruderListCliMenuCommand(ServerInstallationCruder.Create(_parametersManager));
    }
}
