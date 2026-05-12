using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ClearAllGroupsAllSolutionsAllProjects;

// ReSharper disable once ClassNeverInstantiated.Global
public class ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy(
    ILogger<ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(logger, parametersManager);
    }
}
