using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;

// ReSharper disable once UnusedType.Global
public class SelectProjectAllowToolsFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SelectProjectAllowToolsFactoryStrategy(MenuParameters menuParameters, IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(SelectProjectAllowToolsFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _menuParameters.ProjectName);
    }
}
