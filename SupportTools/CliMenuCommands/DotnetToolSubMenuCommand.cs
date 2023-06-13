using System;
using CliMenu;
using CliParameters.MenuCommands;
using LibDataInput;
using SupportTools.Models;

namespace SupportTools.CliMenuCommands;

public class DotnetToolSubMenuCommand : CliMenuCommand
{
    private readonly DotnetTool _tool;

    public DotnetToolSubMenuCommand(DotnetTool tool)
    {
        _tool = tool;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }

    public override CliMenuSet GetSubmenu()
    {
        CliMenuSet dotnetToolSubMenuSet = new($"Dotnet Tool => {_tool.PackageId}");

        ////ყველა ინსტრუმენტის განახლება ბოლო ვერსიამდე
        //var updateAllToolsToLatestVersionCommand = new UpdateAllToolsToLatestVersionCommand();
        //dotnetToolSubMenuSet.AddMenuItem(updateAllToolsToLatestVersionCommand);


        //dotnetToolSubMenuSet.AddMenuItem(new DotnetToolRunCliMenuCommand(_tool, ETestOrReal.Test),
        //               "Run Test Project by this Dotnet Tool");
        //dotnetToolSubMenuSet.AddMenuItem(new DotnetToolRunCliMenuCommand(_tool, ETestOrReal.Real),
        //               "Run REAL Project by this Dotnet Tool");
        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        dotnetToolSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCommand(null, null), key.Length);
        return dotnetToolSubMenuSet;
    }

    protected override string GetStatus()
    {
        var toReturn = _tool.Version;
        if (_tool.AvailableVersion is not null && _tool.AvailableVersion != _tool.Version)
            toReturn += $" => {_tool.AvailableVersion}";
        return toReturn;
    }
}