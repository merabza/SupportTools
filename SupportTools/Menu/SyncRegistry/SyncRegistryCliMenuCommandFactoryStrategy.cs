using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SyncRegistry;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncRegistryCliMenuCommandFactoryStrategy(
    ILogger<SyncRegistryCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncRegistryCliMenuCommand(logger, httpClientFactory, parametersManager);
    }
}
