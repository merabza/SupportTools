using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ExportProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ExportProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ExportProjectCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new ExportProjectCliMenuCommand(_parametersManager, _menuParameters.ProjectName);
    }
}
