using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;

// ReSharper disable once UnusedType.Global
public class DeleteProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly MenuParameters _menuParameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteProjectCliMenuCommandFactoryStrategy(MenuParameters menuParameters)
    {
        _menuParameters = menuParameters;
    }

    public string MenuCommandName => DeleteProjectCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new DeleteProjectCliMenuCommand(parametersManager, _menuParameters.ProjectName);
    }
}
