using System;
using AppCliTools.CliMenu;
using AppCliTools.CliMenu.CliMenuCommands;
using AppCliTools.CliTools.CliMenuCommands;
using AppCliTools.CliTools.Services.MenuBuilder;
using ParametersManagement.LibParameters;
using SupportTools.Menu;

namespace SupportTools;

public sealed class SupportToolsMenuBuilder : IMenuBuilder
{
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportToolsMenuBuilder(IServiceProvider serviceProvider, IParametersManager parametersManager)
    {
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
    }

    public CliMenuSet BuildMainMenu()
    {
        //მთავარი მენიუს ჩატვირთვა
        CliMenuSet mainMenuSet = CliMenuSetFactory.CreateMenuSet("Main Menu", MenuData.MenuCommandNames,
            _serviceProvider, _parametersManager, true);

        return mainMenuSet;
    }
}
