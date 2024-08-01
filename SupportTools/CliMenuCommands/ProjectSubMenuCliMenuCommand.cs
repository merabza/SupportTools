﻿using System;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitData;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager, string projectName) : base(projectName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public override CliMenuSet GetSubmenu()
    {
        var projectSubMenuSet = new CliMenuSet($"Project => {_projectName}");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //პროექტის წაშლა
        var deleteProjectCommand = new DeleteProjectCliMenuCommand(_parametersManager, _projectName);
        projectSubMenuSet.AddMenuItem(deleteProjectCommand);

        //პროექტის ექსპორტი
        var exportProjectCommand = new ExportProjectCliMenuCommand(_parametersManager, _projectName);
        projectSubMenuSet.AddMenuItem(exportProjectCommand);

        //პროექტის პარამეტრი
        var projectCruder = new ProjectCruder(_logger, _parametersManager);
        var editCommand = new EditItemAllFieldsInSequenceCliMenuCommand(projectCruder, _projectName);
        projectSubMenuSet.AddMenuItem(editCommand);

        projectCruder.FillDetailsSubMenu(projectSubMenuSet, _projectName);

        //ყველა პროექტის git-ის სინქრონიზაცია
        var syncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand =
            new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand(_logger, _parametersManager, _projectName);
        projectSubMenuSet.AddMenuItem(syncOneProjectAllGitsWithScaffoldSeedersCliMenuCommand);

        projectSubMenuSet.AddMenuItem(new GitSubMenuCliMenuCommand(_logger, _parametersManager, _projectName,
            EGitCol.Main));

        var project = parameters.GetProject(_projectName);

        if (project != null)
        {
            if (!string.IsNullOrWhiteSpace(project.SeedProjectFilePath))
                projectSubMenuSet.AddMenuItem(new GitSubMenuCliMenuCommand(_logger, _parametersManager, _projectName,
                    EGitCol.ScaffoldSeed));

            //დასაშვები ინსტრუმენტების არჩევა
            projectSubMenuSet.AddMenuItem(new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _projectName));

            foreach (var tool in ToolCommandFabric.ToolsByProjects.Intersect(project.AllowToolsList))
                projectSubMenuSet.AddMenuItem(
                    new ToolTaskCliMenuCommand(_logger, _httpClientFactory, tool, _projectName, null,
                        _parametersManager));
        }

        var serverInfoCruder = new ServerInfoCruder(_logger, _httpClientFactory, _parametersManager, _projectName);

        //ახალი სერვერის ინფორმაციის შექმნა
        var newItemCommand = new NewItemCliMenuCommand(serverInfoCruder, serverInfoCruder.CrudNamePlural,
            $"New {serverInfoCruder.CrudName}");
        projectSubMenuSet.AddMenuItem(newItemCommand);

        //სერვერების ჩამონათვალი
        if (project?.ServerInfos != null)
            foreach (var kvp in project.ServerInfos.OrderBy(o => o.Value.GetItemKey()))
                //, kvp.Value.GetItemKey()
                projectSubMenuSet.AddMenuItem(new ServerInfoSubMenuCliMenuCommand(_logger, _httpClientFactory, kvp.Value.GetItemKey(),
                    _parametersManager, _projectName, kvp.Key));

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        projectSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);

        return projectSubMenuSet;
    }
}