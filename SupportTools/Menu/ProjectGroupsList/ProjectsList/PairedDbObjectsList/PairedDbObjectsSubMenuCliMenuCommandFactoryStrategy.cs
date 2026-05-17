using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class PairedDbObjectsSubMenuCliMenuCommandFactoryStrategy(
    ILogger<PairedDbObjectsSubMenuCliMenuCommand> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new PairedDbObjectsSubMenuCliMenuCommand(logger, parametersManager, menuParameters.ProjectName);
    }
}
