using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;

// ReSharper disable once ClassNeverInstantiated.Global
public class SelectProjectAllowToolsFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SelectProjectAllowToolsFactoryStrategy(SupportToolsMenuParameters menuParameters,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _menuParameters.ProjectName);
    }
}
