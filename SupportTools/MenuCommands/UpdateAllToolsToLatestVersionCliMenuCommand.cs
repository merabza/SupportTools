using CliMenu;
using LibDataInput;
using SupportTools.DotnetTools;

namespace SupportTools.MenuCommands;

public sealed class UpdateAllToolsToLatestVersionCliMenuCommand : CliMenuCommand
{
    // ReSharper disable once ConvertToPrimaryConstructor
    public UpdateAllToolsToLatestVersionCliMenuCommand() : base("Update All Tools To Latest Version")
    {
    }

    protected override void RunAction()
    {

        MenuAction = EMenuAction.Reload;
        if (!Inputer.InputBool("Are you sure, you want to Update All Tools To Latest Version?", true, false))
            return;
        DotnetToolsManager.Instance?.UpdateAllToolsToLatestVersion();
    }
}