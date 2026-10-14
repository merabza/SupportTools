using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3FactoryStrategy(
    ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3FactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV3(logger, httpClientFactory,
            parametersManager, menuParameters.ProjectName);
    }
}
