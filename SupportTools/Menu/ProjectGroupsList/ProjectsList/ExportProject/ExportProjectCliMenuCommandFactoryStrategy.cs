using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ExportProject;

// ReSharper disable once UnusedType.Global
public class ExportProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly MenuParameters _menuParameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ExportProjectCliMenuCommandFactoryStrategy(MenuParameters menuParameters)
    {
        _menuParameters = menuParameters;
    }

    public string MenuCommandName => ExportProjectCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new ExportProjectCliMenuCommand(parametersManager, _menuParameters.ProjectName);
    }
}
