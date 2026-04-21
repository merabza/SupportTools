using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.SupportToolsServerEdit;

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

    public string StrategyName => nameof(ClearAllGroupsAllSolutionsAllProjectsCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(_logger, parametersManager);
    }
}
