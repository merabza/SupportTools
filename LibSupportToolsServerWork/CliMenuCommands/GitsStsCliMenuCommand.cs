using System.Net.Http;
using AppCliTools.CliMenu;
using LibSupportToolsServerWork.Cruders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace LibSupportToolsServerWork.CliMenuCommands;

internal class GitsStsCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitsStsCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
        ParametersManager parametersManager) : base("Gits from SupportToolsServer", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var gitFromServerCruder = GitStsCruder.Create(_logger, _httpClientFactory, _memoryCache, _parametersManager);
        //ჩამოვტვირთოთ გიტ სერვერიდან ინფორმაცია ყველა გიტ პროექტების შესახებ და შემდეგ ეს ინფორმაცია გამოვიყენოთ სიის ჩვენებისას
        var menuSet = gitFromServerCruder.GetListMenu();
        return menuSet;
    }
}