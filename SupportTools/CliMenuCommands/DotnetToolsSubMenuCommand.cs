using System;
using System.Linq;
using CliMenu;
using CliParameters.MenuCommands;
using LibDataInput;
using Microsoft.Extensions.Logging;
using SupportTools.DotnetTools;
using SupportTools.MenuCommands;

namespace SupportTools.CliMenuCommands;

public sealed class DotnetToolsSubMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;

    public DotnetToolsSubMenuCommand(ILogger logger) : base("Dotnet Tools")
    {
        _logger = logger;
        MenuAction = EMenuAction.LoadSubMenu;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }

    public override CliMenuSet GetSubmenu()
    {
        CliMenuSet dotnetToolsSubMenuSet = new("Dotnet Tools");

        //ქვემენიუს ელემენტები

        //ყველას საჭირო ინსტრუმენტის განახლებაზე შემოწმება
        //var dotnetToolsSearched = CreateListOfDotnetToolsInstalled();

        //ყველას საჭირო ინსტრუმენტის განახლება ან დაყენება
        //dotnet tool update --global dotnet-ef
        //dotnet tool install --global dotnet-ef
        //var dotnetAllToolsCreateOrUpdateCommand = new DotnetAllToolsCreateOrUpdateCommand();

        //ყველა ინსტრუმენტის განახლება ბოლო ვერსიამდე
        var updateAllToolsToLatestVersionCommand = new UpdateAllToolsToLatestVersionCommand();
        dotnetToolsSubMenuSet.AddMenuItem(updateAllToolsToLatestVersionCommand);


        //დაინსტალირებული ინსტრუმენტების სიის დადგენა
        var dotnetToolsInstalled = DotnetToolsManager.Instance.DotnetTools;

        //დაინსტალირებული ინსტრუმენტების სიის გამოტანა მენიუში
        foreach (var tool in dotnetToolsInstalled.OrderBy(x => x.PackageId))
            dotnetToolsSubMenuSet.AddMenuItem(new DotnetToolSubMenuCommand(tool), tool.PackageId);

        //var dotnetToolCommand = new DotnetToolCliMenuCommand();
        //dotnetToolsSubMenuSet.AddMenuItem(dotnetToolCommand);


        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        dotnetToolsSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCommand(null, null),
            key.Length);


        return dotnetToolsSubMenuSet;
    }
}