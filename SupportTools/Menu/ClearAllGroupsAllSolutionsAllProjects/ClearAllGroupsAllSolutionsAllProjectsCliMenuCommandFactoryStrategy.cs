using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ClearAllGroupsAllSolutionsAllProjects;

// ReSharper disable once UnusedType.Global
public class ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy(
        ILogger<ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string MenuCommandName => ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(_logger, parametersManager);
    }
}
