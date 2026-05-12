using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.UpdateOutdatedPackages;

// ReSharper disable once ClassNeverInstantiated.Global
public class UpdateOutdatedPackagesCliMenuCommandFactoryStrategy(
    ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new UpdateOutdatedPackagesCliMenuCommand(logger, parametersManager1);
    }
}
