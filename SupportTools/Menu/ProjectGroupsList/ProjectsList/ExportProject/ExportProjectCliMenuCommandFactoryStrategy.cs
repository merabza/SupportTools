using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ExportProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ExportProjectCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new ExportProjectCliMenuCommand(parametersManager, menuParameters.ProjectName);
    }
}
