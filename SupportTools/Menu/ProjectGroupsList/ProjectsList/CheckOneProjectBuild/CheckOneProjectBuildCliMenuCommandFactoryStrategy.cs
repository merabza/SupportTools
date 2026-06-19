using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectBuild;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckOneProjectBuildCliMenuCommandFactoryStrategy(
    ILogger<CheckOneProjectBuildCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckOneProjectBuildCliMenuCommand(logger, (ParametersManager)parametersManager, menuParameters,
            menuParameters.ProjectName);
    }
}
