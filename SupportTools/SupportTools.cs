using System;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using CliTools;
using CliTools.CliMenuCommands;
using LibDataInput;
using LibGitWork.CliMenuCommands;
using LibParameters;
using LibTools.CliMenuCommands;
using Microsoft.Extensions.Logging;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools;

public sealed class SupportTools : CliAppLoop
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SupportTools(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager) :
        base((IParametersWithRecentData)parametersManager.Parameters)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet BuildMainMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //მთავარი მენიუს ჩატვირთვა
        var mainMenuSet = new CliMenuSet("Main Menu");

        //პარამეტრების რედაქტორი
        var supportToolsParametersEditor =
            new SupportToolsParametersEditor(_logger, _httpClientFactory, parameters, _parametersManager);
        mainMenuSet.AddMenuItem(new ParametersEditorListCliMenuCommand(supportToolsParametersEditor));

        var dotnetToolsSubMenuCommand = new DotnetToolsSubMenuCliMenuCommand();
        mainMenuSet.AddMenuItem(dotnetToolsSubMenuCommand);

        //ახალი პროექტების შემქმნელი სუბმენიუ
        mainMenuSet.AddMenuItem(
            new ProjectCreatorSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager));

        var projectCruder = new ProjectCruder(_logger, _httpClientFactory, _parametersManager);

        //ახალი პროექტის შექმნა
        var newItemCommand = new NewItemCliMenuCommand(projectCruder, projectCruder.CrudNamePlural,
            $"New {projectCruder.CrudName}");
        mainMenuSet.AddMenuItem(newItemCommand);

        //პროექტის დაიმპორტება
        var importProjectCommand = new ImportProjectCliMenuCommand(_parametersManager);
        mainMenuSet.AddMenuItem(importProjectCommand);

        ////ყველა პროექტის git-ის სინქრონიზაცია
        //var syncAllProjectsGits = new SyncAllProjectsAllGitsCliMenuCommand(_logger, _parametersManager);
        //mainMenuSet.AddMenuItem(syncAllProjectsGits);

        //ყველა პროექტის git-ის სინქრონიზაცია ახალი მეთოდით V2
        var syncAllProjectsGitsV2 = new SyncAllProjectsAllGitsCliMenuCommandV2(_logger, _parametersManager);
        mainMenuSet.AddMenuItem(syncAllProjectsGitsV2);

        //ყველა პროექტის პაკეტების განახლება
        var updateOutdatedPackages = new UpdateOutdatedPackagesCliMenuCommand(_logger, _parametersManager);
        mainMenuSet.AddMenuItem(updateOutdatedPackages);

        //ყველა პროექტის bin obj და სხვა ზედმეტი ფაილებისა და ფოლდერებისგან გასუფთავება
        var clearAllProjectsCliMenuCommand =
            new ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand(_logger, _parametersManager);
        mainMenuSet.AddMenuItem(clearAllProjectsCliMenuCommand);

        //პროექტების ჯგუფების ჩამონათვალი
        foreach (var projectGroupName in parameters.Projects
                     .Select(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName)).Distinct()
                     .OrderBy(x => x))
            mainMenuSet.AddMenuItem(new ProjectGroupSubMenuCliMenuCommand(_logger, _httpClientFactory,
                _parametersManager, projectGroupName));

        //ბოლოს გამოყენებული ბრძანებები
        foreach (var itemSubMenuCommand in GetRecentCommands())
            mainMenuSet.AddMenuItem(itemSubMenuCommand);

        //პროგრამიდან გასასვლელი
        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, new ExitCliMenuCommand(), key.Length);
        return mainMenuSet;
    }
}