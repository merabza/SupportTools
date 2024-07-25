using CliMenu;

namespace SupportTools.CliMenuCommands;

public class InfoCliMenuCommand : CliMenuCommand
{
    private readonly string _menuLink;

    // ReSharper disable once ConvertToPrimaryConstructor
    public InfoCliMenuCommand(string info, string menuLink) : base(info, EMenuAction.GoToMenuLink,
        EMenuAction.GoToMenuLink, null, false, EStatusView.Brackets, true)
    {
        _menuLink = menuLink;
    }

    public override string GetMenuLinkToGo()
    {
        return _menuLink;
    }
}