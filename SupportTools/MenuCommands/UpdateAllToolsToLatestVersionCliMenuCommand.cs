using System;
using CliMenu;
using LibDataInput;
using SupportTools.DotnetTools;
using SystemToolsShared;

namespace SupportTools.MenuCommands;

public sealed class UpdateAllToolsToLatestVersionCliMenuCommand : CliMenuCommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateAllToolsToLatestVersionCliMenuCommand() : base("Update All Tools To Latest Version")
    {
    }

    protected override void RunAction()
    {
        try
        {
            MenuAction = EMenuAction.Reload;
            if (!Inputer.InputBool("Are you sure, you want to Update All Tools To Latest Version?", true, false))
                return;
            DotnetToolsManager.Instance?.UpdateAllToolsToLatestVersion();
        }
        catch (DataInputEscapeException)
        {
            Console.WriteLine();
            Console.WriteLine("Escape... ");
            //StShared.Pause();
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }
    }
}