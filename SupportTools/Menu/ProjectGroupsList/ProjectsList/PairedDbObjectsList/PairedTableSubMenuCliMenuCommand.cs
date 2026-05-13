using System;
using AppCliTools.CliMenu;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PairedDbObjectsList;

public sealed class PairedTableSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly string _pairedTableKey;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PairedTableSubMenuCliMenuCommand(IServiceProvider serviceProvider, string pairedTableKey, string displayName,
        SupportToolsMenuParameters menuParameters) : base(displayName, EMenuAction.LoadSubMenu)
    {
        _serviceProvider = serviceProvider;
        _pairedTableKey = pairedTableKey;
        _menuParameters = menuParameters;
    }

    public override CliMenuSet? GetSubMenu()
    {
        _menuParameters.PairedTableKey = _pairedTableKey;
        _menuParameters.PairedFieldKey = null;

        return CliMenuSetFactory.CreateMenuSet($"Table pair: {_pairedTableKey}",
            MenuData.PairedTableSubMenuCommandFactoryStrategyNames, _serviceProvider);
    }
}
