using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckOneProjectSolution;

// ReSharper disable once ClassNeverInstantiated.Global
public class CheckOneProjectSolutionCliMenuCommandFactoryStrategy(
    ILogger<CheckOneProjectSolutionCliMenuCommandFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CheckOneProjectSolutionCliMenuCommand(logger, parametersManager, menuParameters,
            menuParameters.ProjectName);
    }
}
