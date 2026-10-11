using System;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.Services.MenuBuilder;
using Microsoft.Extensions.DependencyInjection;
using SupportTools.AutoSync;
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
        //გაშვების ავტოსინქრონიზაციის შეჯამება (D2). მენიუმ ეკრანი ახლახან გაასუფთავა და სათაური დაწერა, ამიტომ ის
        //სათაურის ქვეშ ჩანს. ის მხოლოდ პირველ აგებაზე იწერება
        _serviceProvider.GetService<RegistryAutoSync>()?.WriteDeferredOutput();

        //მთავარი მენიუს ჩატვირთვა
        return Task.FromResult(CliMenuSetFactory.CreateMenuSet("Main Menu",
            MenuData.MainMenuCommandFactoryStrategyNames, _serviceProvider, true));
    }
}
