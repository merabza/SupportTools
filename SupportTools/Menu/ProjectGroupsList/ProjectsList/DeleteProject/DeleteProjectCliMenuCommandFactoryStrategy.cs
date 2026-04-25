using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class DeleteProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DeleteProjectCliMenuCommandFactoryStrategy(SupportToolsMenuParameters menuParameters,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new DeleteProjectCliMenuCommand(_parametersManager, _menuParameters.ProjectName);
    }
}
