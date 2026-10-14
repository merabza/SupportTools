using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SyncAllProjectsAllGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncAllProjectsAllGitsCliMenuCommandV3FactoryStrategy(
    ILogger<SyncAllProjectsAllGitsCliMenuCommandV3FactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncAllProjectsAllGitsCliMenuCommandV3(logger, httpClientFactory, parametersManager);
    }
}
