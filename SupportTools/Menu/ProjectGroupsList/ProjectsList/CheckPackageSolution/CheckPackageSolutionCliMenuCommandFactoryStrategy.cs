using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckPackageSolutionCliMenuCommandFactoryStrategy(
    ILogger<CheckPackageSolutionCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckPackageSolutionCliMenuCommand(logger, parametersManager, menuParameters.ProjectName);
    }
}
