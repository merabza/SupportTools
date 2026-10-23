using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneProjectAllGitsCliMenuCommandV3FactoryStrategy(
    ILogger<SyncOneProjectAllGitsCliMenuCommandV3FactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneProjectAllGitsCliMenuCommandV3(logger, httpClientFactory, parametersManager,
            menuParameters.ProjectName);
    }
}
