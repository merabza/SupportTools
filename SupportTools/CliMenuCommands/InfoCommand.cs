using CliMenu;

namespace SupportTools.CliMenuCommands;

public class InfoCommand : CliMenuCommand
{
    private readonly string _menuUrlPrefix;

    public InfoCommand(string info, string menuUrlPrefix) : base(info, null, false, EStatusView.Brackets, true)
    {
        _menuUrlPrefix = menuUrlPrefix;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.GoToMenuLink;
    }

    public override string GetMenuLinkToGo()
    {
        return $"{_menuUrlPrefix}{Name}";
    }
}