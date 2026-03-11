using System.Net.Http;
using AppCliTools.CliMenu;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibSupportToolsServerWork.CliMenuCommands;

internal sealed class GitIgnoreFileTypesStsCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitIgnoreFileTypesStsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache, ParametersManager parametersManager) : base("GitIgnore File Types",
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
            GitIgnoreFileTypesStsCruder.Create(_logger, _httpClientFactory, _memoryCache, _parametersManager);

        CliMenuSet menuSet = gitIgnoreFilePathsStsCruder.GetListMenu();
        return menuSet;
    }
}
