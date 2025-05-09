using System;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using SupportTools.Models;

namespace SupportTools.CliMenuCommands;

public sealed class DotnetToolSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly DotnetTool _tool;

    // ReSharper disable once ConvertToPrimaryConstructor
    public DotnetToolSubMenuCliMenuCommand(DotnetTool tool) : base(tool.PackageId, EMenuAction.LoadSubMenu)
    {
        _tool = tool;
    }

    public override CliMenuSet GetSubMenu()
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
        dotnetToolSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null),
            key.Length);
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