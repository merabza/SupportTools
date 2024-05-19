using CliMenu;

namespace SupportTools.CliMenuCommands;

public class InfoCliMenuCommand : CliMenuCommand
{
    private readonly string _menuUrlPrefix;

    // ReSharper disable once ConvertToPrimaryConstructor
    public InfoCliMenuCommand(string info, string menuUrlPrefix) : base(info, EMenuAction.GoToMenuLink,
        EMenuAction.GoToMenuLink, null, false, EStatusView.Brackets, true)
    {
        _menuUrlPrefix = menuUrlPrefix;
    }

    public override string GetMenuLinkToGo()
    {
        return $"{_menuUrlPrefix}{Name}";
    }
}