using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.DeleteProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class DeleteProjectCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new DeleteProjectCliMenuCommand(parametersManager, menuParameters.ProjectName);
    }
}
