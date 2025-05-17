using System.Net.Http;
using CliMenu;
using LibParameters;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LibSupportToolsServerWork.CliMenuCommands;

internal class GitIgnoreFilePathsStsCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnoreFilePathsStsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, ParametersManager parametersManager) : base("GitIgnore File Paths from SupportToolsServer",
        EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var gitIgnoreFilePathsStsCruder =
            GitIgnoreFilePathsStsCruder.Create(_logger, _httpClientFactory, _memoryCache, _parametersManager);

        var menuSet = gitIgnoreFilePathsStsCruder.GetListMenu();
        return menuSet;
    }

}