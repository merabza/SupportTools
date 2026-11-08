using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ReversePackageDistributionInGroup;

// ReSharper disable once ClassNeverInstantiated.Global
public class ReversePackageDistributionInGroupCliMenuCommandFactoryStrategy(
    ILogger<ReversePackageDistributionInGroupCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new ReversePackageDistributionInGroupCliMenuCommand(logger, parametersManager,
            menuParameters.ProjectGroupName);
    }
}
