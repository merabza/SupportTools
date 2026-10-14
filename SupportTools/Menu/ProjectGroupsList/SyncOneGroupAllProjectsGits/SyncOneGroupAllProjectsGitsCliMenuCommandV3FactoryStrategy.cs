using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy(
    ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV3FactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneGroupAllProjectsGitsCliMenuCommandV3(logger, httpClientFactory, parametersManager,
            menuParameters.ProjectGroupName);
    }
}
