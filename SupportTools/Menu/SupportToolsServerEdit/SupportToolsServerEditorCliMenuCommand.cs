using System.Net.Http;
using AppCliTools.CliMenu;
using LibSupportToolsServerWork.CliMenuCommands;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SupportToolsServerEdit;

public sealed class SupportToolsServerEditorCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServerEditorCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, IParametersManager parametersManager) : base(MenuCommandName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
    }

    public static string MenuCommandName => "Support Tools Server Editor";

    public override CliMenuSet GetSubMenu()
    {
        var mainMenuSet = new CliMenuSet();

        var gitIgnoreFilePathsStsCliMenuCommand =
            new GitIgnoreFileTypesStsCliMenuCommand(_logger, _httpClientFactory, _memoryCache, _parametersManager);
        mainMenuSet.AddMenuItem(gitIgnoreFilePathsStsCliMenuCommand);

        var gitsSupportToolsServerCliMenuCommand =
            new GitsStsCliMenuCommand(_logger, _httpClientFactory, _memoryCache, _parametersManager);
        mainMenuSet.AddMenuItem(gitsSupportToolsServerCliMenuCommand);

        mainMenuSet.AddEscapeCommand();

        return mainMenuSet;
    }
}
