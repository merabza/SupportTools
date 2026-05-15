using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class EditPairedTableSeedDataTypeCliMenuCommandFactoryStrategy(
    ILogger<EditPairedTableSeedDataTypeCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new EditPairedTableSeedDataTypeCliMenuCommand(logger, parametersManager, menuParameters);
    }
}
