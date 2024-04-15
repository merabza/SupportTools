using System;
using System.Linq;
using CliMenu;
using CliParameters.CliMenuCommands;
using CliTools;
using CliTools.CliMenuCommands;
using LibDataInput;
using LibGitWork.CliMenuCommands;
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

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportTools(ILogger logger, ParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool BuildMainMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მთავარი მენიუს ჩატვირთვა
        var mainMenuSet = new CliMenuSet("Main Menu");
        AddChangeMenu(mainMenuSet);

        //პარამეტრების რედაქტორი
        var supportToolsParametersEditor = new SupportToolsParametersEditor(_logger, parameters, _parametersManager);
        mainMenuSet.AddMenuItem(new ParametersEditorListCliMenuCommand(supportToolsParametersEditor),
            "Support Tools Parameters Editor");

        var dotnetToolsSubMenuCommand = new DotnetToolsSubMenuCliMenuCommand();
        mainMenuSet.AddMenuItem(dotnetToolsSubMenuCommand);

        //ახალი პროექტების შემქმნელი სუბმენიუ
        mainMenuSet.AddMenuItem(new ProjectCreatorSubMenuCliMenuCommand(_logger, _parametersManager));

        var projectCruder = new ProjectCruder(_logger, _parametersManager);

        //ახალი პროექტის შექმნა
        var newItemCommand =
            new NewItemCliMenuCommand(projectCruder, projectCruder.CrudNamePlural, $"New {projectCruder.CrudName}");
        mainMenuSet.AddMenuItem(newItemCommand);

        //პროექტის დაიმპორტება
        var importProjectCommand = new ImportProjectCliMenuCommand(_parametersManager);
        mainMenuSet.AddMenuItem(importProjectCommand);

        //ყველა პროექტის git-ის სინქრონიზაცია
        var syncAllProjectsGits = new SyncAllProjectsAllGitsCliMenuCommand(_logger, _parametersManager);
        mainMenuSet.AddMenuItem(syncAllProjectsGits);

        //პროექტების ჯგუფების ჩამონათვალი
        foreach (var projectGroupName in parameters.Projects
                     .Select(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName)).Distinct()
                     .OrderBy(x => x))
            mainMenuSet.AddMenuItem(new ProjectGroupSubMenuCliMenuCommand(_logger, _parametersManager, projectGroupName),
                projectGroupName);
        

        ////პროექტების ჩამონათვალი
        //foreach (var kvp in parameters.Projects.OrderBy(o => o.Key))
        //    mainMenuSet.AddMenuItem(new ProjectSubMenuCommand(_logger, _parametersManager, kvp.Key), kvp.Key);

        //პროგრამიდან გასასვლელი
        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, "Exit", new ExitCliMenuCommand(), key.Length);

        return true;
    }
}