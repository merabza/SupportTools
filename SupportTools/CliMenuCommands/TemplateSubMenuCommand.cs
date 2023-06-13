using System;
using CliMenu;
using CliParameters.MenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData;

namespace SupportTools.CliMenuCommands;

public sealed class TemplateSubMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _templateName;

    public TemplateSubMenuCommand(ILogger logger, ParametersManager parametersManager, string templateName) :
        base(templateName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _templateName = templateName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }

    public override CliMenuSet GetSubmenu()
    {
        CliMenuSet templateSubMenuSet = new($"Template => {_templateName}");

        //პროექტის წაშლა
        DeleteTemplateCliMenuCommand deleteTemplateCommand = new(_parametersManager, _templateName);
        templateSubMenuSet.AddMenuItem(deleteTemplateCommand);

        //პროექტის პარამეტრი
        TemplateCruder templateCruder = new(_logger, _parametersManager);
        EditItemAllFieldsInSequenceCommand editCommand = new(templateCruder, _templateName);
        templateSubMenuSet.AddMenuItem(editCommand, "Edit All fields in sequence");

        templateCruder.FillDetailsSubMenu(templateSubMenuSet, _templateName);

        templateSubMenuSet.AddMenuItem(
            new TemplateRunCliMenuCommand(_logger, _parametersManager, _templateName, ETestOrReal.Test),
            "Create Test Project by this Template");
        templateSubMenuSet.AddMenuItem(
            new TemplateRunCliMenuCommand(_logger, _parametersManager, _templateName, ETestOrReal.Real),
            "Create REAL Project by this Template");

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        templateSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCommand(null, null), key.Length);

        return templateSubMenuSet;
    }
}