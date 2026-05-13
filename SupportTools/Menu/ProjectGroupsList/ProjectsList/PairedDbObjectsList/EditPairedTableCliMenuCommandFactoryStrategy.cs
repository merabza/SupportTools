using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class EditPairedTableCliMenuCommandFactoryStrategy(
    ILogger<EditPairedTableCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new EditPairedTableCliMenuCommand(logger, parametersManager, menuParameters);
    }
}
