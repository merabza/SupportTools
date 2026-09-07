using System;
using System.Threading.Tasks;
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

    public Task<CliMenuSet?> BuildMainMenu()
    {
        //მთავარი მენიუს ჩატვირთვა
        return Task.FromResult(CliMenuSetFactory.CreateMenuSet("Main Menu",
            MenuData.MainMenuCommandFactoryStrategyNames, _serviceProvider, true));
    }
}
