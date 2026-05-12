using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy(
    ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(logger, parametersManager,
            menuParameters.ProjectGroupName);
    }
}
