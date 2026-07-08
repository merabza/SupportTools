using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;

// ReSharper disable once ClassNeverInstantiated.Global
public class BuildPackageCliMenuCommandFactoryStrategy(
    ILogger<BuildPackageCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new BuildPackageCliMenuCommand(logger, (ParametersManager)parametersManager,
            menuParameters.ProjectName);
    }
}
