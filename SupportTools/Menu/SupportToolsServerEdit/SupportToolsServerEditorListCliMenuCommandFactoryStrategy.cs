using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SupportToolsServerEdit;

// ReSharper disable once ClassNeverInstantiated.Global
public class SupportToolsServerEditorListCliMenuCommandFactoryStrategy(
    ILogger<SupportToolsServerEditorListCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SupportToolsServerEditorCliMenuCommand(logger, httpClientFactory, memoryCache, parametersManager);
    }
}
