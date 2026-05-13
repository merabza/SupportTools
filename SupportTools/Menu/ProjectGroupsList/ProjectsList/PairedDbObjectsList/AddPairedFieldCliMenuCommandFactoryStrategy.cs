using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class AddPairedFieldCliMenuCommandFactoryStrategy(
    ILogger<AddPairedFieldCliMenuCommandFactoryStrategy> logger,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new AddPairedFieldCliMenuCommand(logger, parametersManager, menuParameters);
    }
}
