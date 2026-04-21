using System;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.Services.MenuBuilder;
using SupportTools.Menu;

namespace SupportTools;

public sealed class SupportToolsMenuBuilder : IMenuBuilder
{
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsMenuBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public CliMenuSet BuildMainMenu()
    {
        //მთავარი მენიუს ჩატვირთვა
        return CliMenuSetFactory.CreateMenuSet("Main Menu", MenuData.MenuCommandNames, _serviceProvider, true);
    }
}
