using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ReversePackageDistribution;

// ReSharper disable once ClassNeverInstantiated.Global
public class ReversePackageDistributionCliMenuCommandFactoryStrategy(
    ILogger<ReversePackageDistributionCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new ReversePackageDistributionCliMenuCommand(logger, parametersManager1);
    }
}
