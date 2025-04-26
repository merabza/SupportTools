using System;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace LibSupportToolsServerWork.CliMenuCommands;

public class SupportToolsServerEditorCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ParametersManager _parametersManager;

    public SupportToolsServerEditorCliMenuCommand(ILogger logger, IHttpClientFactory? httpClientFactory, ParametersManager parametersManager) : base(
        "Support Tools Server Editor", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubmenu()
    {
        var mainMenuSet = new CliMenuSet();

        //ყველა პროექტის git-ის სინქრონიზაცია ახალი მეთოდით V2
        var gitsSupportToolsServerCliMenuCommand = new GitsSupportToolsServerCliMenuCommand(_logger, _httpClientFactory, _parametersManager);
        mainMenuSet.AddMenuItem(gitsSupportToolsServerCliMenuCommand);


        //პროგრამიდან გასასვლელი
        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);
        return mainMenuSet;
    }
}