using System;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData;

namespace SupportTools.CliMenuCommands;

public sealed class TemplateSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _templateName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TemplateSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string templateName) : base(templateName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _templateName = templateName;
        _httpClientFactory = httpClientFactory;
    }

    public override CliMenuSet GetSubmenu()
    {
        CliMenuSet templateSubMenuSet = new($"Template => {_templateName}");

        //პროექტის წაშლა
        DeleteTemplateCliMenuCommand deleteTemplateCommand = new(_parametersManager, _templateName);
        templateSubMenuSet.AddMenuItem(deleteTemplateCommand);

        //პროექტის პარამეტრი
        TemplateCruder templateCruder = new(_logger, _parametersManager);
        EditItemAllFieldsInSequenceCliMenuCommand editCommand = new(templateCruder, _templateName);
        templateSubMenuSet.AddMenuItem(editCommand);

        templateCruder.FillDetailsSubMenu(templateSubMenuSet, _templateName);

        templateSubMenuSet.AddMenuItem(new TemplateRunCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _templateName, ETestOrReal.Test));

        templateSubMenuSet.AddMenuItem(new TemplateRunCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _templateName, ETestOrReal.Real));

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        templateSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null),
            key.Length);

        return templateSubMenuSet;
    }
}