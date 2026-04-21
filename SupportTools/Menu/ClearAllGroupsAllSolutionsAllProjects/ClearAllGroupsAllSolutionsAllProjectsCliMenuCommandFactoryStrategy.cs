using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ClearAllGroupsAllSolutionsAllProjects;

// ReSharper disable once ClassNeverInstantiated.Global
public class ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    public ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy(
        ILogger<ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy> logger,
        IParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand()
    {
        return new ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(_logger, _parametersManager);
    }
}
