using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SelectProjectAllowTools;

// ReSharper disable once ClassNeverInstantiated.Global
public class SelectProjectAllowToolsFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SelectProjectAllowToolsCliMenuCommand(parametersManager, menuParameters.ProjectName);
    }
}
