using CliMenu;

namespace SupportTools.CliMenuCommands;

public class InfoCommand : CliMenuCommand
{
    public InfoCommand(string info) : base(info, null, false, EStatusView.Brackets, true)
    {

    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;
    }

}