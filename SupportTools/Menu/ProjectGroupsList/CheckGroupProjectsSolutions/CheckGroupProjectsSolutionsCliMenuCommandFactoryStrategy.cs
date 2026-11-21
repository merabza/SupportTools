using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.CheckGroupProjectsSolutions;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckGroupProjectsSolutionsCliMenuCommandFactoryStrategy(
    ILogger<CheckGroupProjectsSolutionsCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckGroupProjectsSolutionsCliMenuCommand(logger, parametersManager, menuParameters,
            menuParameters.ProjectGroupName);
    }
}
