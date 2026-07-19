using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.RemoveUnusedPackageVersions;

// ReSharper disable once ClassNeverInstantiated.Global
public class RemoveUnusedPackageVersionsCliMenuCommandFactoryStrategy(
    ILogger<RemoveUnusedPackageVersionsCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new RemoveUnusedPackageVersionsCliMenuCommand(logger, parametersManager1);
    }
}
