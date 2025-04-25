using System;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using Microsoft.Extensions.Logging;

namespace LibSupportToolsServerWork.CliMenuCommands;

public class SupportToolsServerEditorCliMenuCommand : CliMenuCommand
{
    public SupportToolsServerEditorCliMenuCommand(ILogger logger, IHttpClientFactory? httpClientFactory) : base(
        "Support Tools Server Editor", EMenuAction.LoadSubMenu)
    {
    }

    public override CliMenuSet GetSubmenu()
    {
        var mainMenuSet = new CliMenuSet();
        
        //ყველა პროექტის git-ის სინქრონიზაცია ახალი მეთოდით V2
        var syncAllProjectsGitsV2 = new GitsSupportToolsServerCliMenuCommand();
        mainMenuSet.AddMenuItem(syncAllProjectsGitsV2);


        //პროგრამიდან გასასვლელი
        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);
        return mainMenuSet;
    }
}