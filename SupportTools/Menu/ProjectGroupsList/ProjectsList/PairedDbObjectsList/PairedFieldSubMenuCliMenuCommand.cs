using System;
using AppCliTools.CliMenu;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class PairedFieldSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly string _pairedFieldKey;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedFieldSubMenuCliMenuCommand(IServiceProvider serviceProvider, string pairedFieldKey, string displayName,
        SupportToolsMenuParameters menuParameters) : base(displayName, EMenuAction.LoadSubMenu)
    {
        _serviceProvider = serviceProvider;
        _pairedFieldKey = pairedFieldKey;
        _menuParameters = menuParameters;
    }

    public override CliMenuSet? GetSubMenu()
    {
        _menuParameters.PairedFieldKey = _pairedFieldKey;

        return CliMenuSetFactory.CreateMenuSet($"Field pair: {_pairedFieldKey}",
            MenuData.PairedFieldSubMenuCommandFactoryStrategyNames, _serviceProvider);
    }
}
