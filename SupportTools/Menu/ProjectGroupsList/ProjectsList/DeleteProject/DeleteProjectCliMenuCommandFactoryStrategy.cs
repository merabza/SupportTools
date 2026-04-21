using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class DeleteProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    public DeleteProjectCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(DeleteProjectCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand()
    {
        return new DeleteProjectCliMenuCommand(_parametersManager, _menuParameters.ProjectName);
    }
}
