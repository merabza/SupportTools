using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneProjectAllGitsCliMenuCommandV2FactoryStrategy(
    ILogger<SyncOneProjectAllGitsCliMenuCommandV2FactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneProjectAllGitsCliMenuCommandV2(logger, parametersManager, menuParameters.ProjectName);
    }
}
