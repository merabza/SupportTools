using System;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class PairedDbObjectsSubMenuCliMenuCommandFactoryStrategy(
    IServiceProvider serviceProvider,
    IParametersManager parametersManager,
    SupportToolsMenuParameters menuParameters) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new PairedDbObjectsSubMenuCliMenuCommand(serviceProvider, parametersManager, menuParameters.ProjectName,
            menuParameters);
    }
}
