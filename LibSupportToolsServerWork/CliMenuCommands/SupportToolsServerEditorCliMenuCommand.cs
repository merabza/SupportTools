using System;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibSupportToolsServerWork.CliMenuCommands;

public sealed class SupportToolsServerEditorCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsServerEditorCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, ParametersManager parametersManager) : base("Support Tools Server Editor",
        EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var mainMenuSet = new CliMenuSet();

        var gitIgnoreFilePathsStsCliMenuCommand =
            new GitIgnoreFileTypesStsCliMenuCommand(_logger, _httpClientFactory, _memoryCache, _parametersManager);
        mainMenuSet.AddMenuItem(gitIgnoreFilePathsStsCliMenuCommand);

        var gitsSupportToolsServerCliMenuCommand =
            new GitsStsCliMenuCommand(_logger, _httpClientFactory, _memoryCache, _parametersManager);
        mainMenuSet.AddMenuItem(gitsSupportToolsServerCliMenuCommand);

        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);
        return mainMenuSet;
    }
}