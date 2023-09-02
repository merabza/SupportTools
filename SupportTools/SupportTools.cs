using System;
using System.Linq;
using CliMenu;
using CliParameters.MenuCommands;
using CliTools;
using CliTools.Commands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools;

public sealed class SupportTools : CliAppLoop
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    public SupportTools(ILogger logger, ParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool BuildMainMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მთავარი მენიუს ჩატვირთვა
        CliMenuSet mainMenuSet = new("Main Menu");
        AddChangeMenu(mainMenuSet);

        //პარამეტრების რედაქტორი
        SupportToolsParametersEditor supportToolsParametersEditor = new(_logger, parameters, _parametersManager);
        mainMenuSet.AddMenuItem(new ParametersEditorListCommand(supportToolsParametersEditor),
            "Support Tools Parameters Editor");

        DotnetToolsSubMenuCommand dotnetToolsSubMenuCommand = new();
        mainMenuSet.AddMenuItem(dotnetToolsSubMenuCommand);

        //ახალი პროექტების შემქმნელი სუბმენიუ
        mainMenuSet.AddMenuItem(new ProjectCreatorSubMenuCommand(_logger, _parametersManager));

        ProjectCruder projectCruder = new(_logger, _parametersManager);

        //ახალი პროექტის შექმნა
        NewItemCommand newItemCommand =
            new(projectCruder, projectCruder.CrudNamePlural, $"New {projectCruder.CrudName}");
        mainMenuSet.AddMenuItem(newItemCommand);

        //პროექტის დაიმპორტება
        ImportProjectCliMenuCommand importProjectCommand = new(_parametersManager);
        mainMenuSet.AddMenuItem(importProjectCommand);

        //პროექტების ჩამონათვალი
        foreach (var kvp in parameters.Projects.OrderBy(o => o.Key))
            mainMenuSet.AddMenuItem(new ProjectSubMenuCommand(_logger, _parametersManager, kvp.Key), kvp.Key);

        //პროგრამიდან გასასვლელი
        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, "Exit", new ExitCommand(), key.Length);

        return true;
    }
}