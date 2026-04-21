using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SupportToolsServerEdit;

// ReSharper disable once ClassNeverInstantiated.Global
public class SupportToolsServerEditorListCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupportToolsServerEditorListCliMenuCommandFactoryStrategy> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServerEditorListCliMenuCommandFactoryStrategy(
        ILogger<SupportToolsServerEditorListCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, IParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new SupportToolsServerEditorCliMenuCommand(_logger, _httpClientFactory, _memoryCache,
            _parametersManager);
    }
}
