using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ReversePackageDistributionInProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ReversePackageDistributionInProjectCliMenuCommandFactoryStrategy(
    ILogger<ReversePackageDistributionInProjectCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new ReversePackageDistributionInProjectCliMenuCommand(logger, parametersManager,
            menuParameters.ProjectName);
    }
}
