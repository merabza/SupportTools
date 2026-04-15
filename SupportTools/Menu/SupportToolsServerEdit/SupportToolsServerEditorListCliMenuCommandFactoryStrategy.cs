using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SupportToolsServerEdit;

public class SupportToolsServerEditorListCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupportToolsServerEditorListCliMenuCommandFactoryStrategy> _logger;
    private readonly IMemoryCache _memoryCache;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServerEditorListCliMenuCommandFactoryStrategy(
        ILogger<SupportToolsServerEditorListCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
    }

    public string MenuCommandName => SupportToolsServerEditorCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new SupportToolsServerEditorCliMenuCommand(_logger, _httpClientFactory, _memoryCache, parametersManager);
    }
}
