using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.CheckAllProjectsSolutions;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckAllProjectsSolutionsCliMenuCommandFactoryStrategy(
    ILogger<CheckAllProjectsSolutionsCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckAllProjectsSolutionsCliMenuCommand(logger, parametersManager, menuParameters);
    }
}
