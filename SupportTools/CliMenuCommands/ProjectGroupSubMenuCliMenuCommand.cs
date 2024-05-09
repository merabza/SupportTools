using System;
using System.Linq;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectGroupSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectGroupName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupSubMenuCliMenuCommand(ILogger logger, ParametersManager parametersManager,
        string projectGroupName) :
        base(projectGroupName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }


    public override CliMenuSet GetSubmenu()
    {
        var projectGroupSubMenuSet = new CliMenuSet($"Projects Group => {_projectGroupName}");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ////პროექტის წაშლა
        //var deleteProjectCommand = new DeleteProjectCliMenuCommand(_parametersManager, _projectGroupName);
        //projectGroupSubMenuSet.AddMenuItem(deleteProjectCommand);

        ////პროექტის ექსპორტი
        //var exportProjectCommand = new ExportProjectCliMenuCommand(_parametersManager, _projectGroupName);
        //projectGroupSubMenuSet.AddMenuItem(exportProjectCommand);

        ////პროექტის პარამეტრი
        //var projectCruder = new ProjectCruder(_logger, _parametersManager);
        //var editCommand = new EditItemAllFieldsInSequenceCommand(projectCruder, _projectGroupName);
        //projectGroupSubMenuSet.AddMenuItem(editCommand, "Edit All fields in sequence");

        //projectCruder.FillDetailsSubMenu(projectGroupSubMenuSet, _projectGroupName);

        //projectGroupSubMenuSet.AddMenuItem(new GitSubMenuCommand(_logger, _parametersManager, _projectGroupName, EGitCol.Main),
        //    "Git");


        //ყველა პროექტის git-ის სინქრონიზაცია
        var syncGroupAllProjectsGits =
            new SyncOneGroupAllProjectsGitsCliMenuCommand(_logger, _parametersManager, _projectGroupName);
        projectGroupSubMenuSet.AddMenuItem(syncGroupAllProjectsGits);


        //var project = parameters.GetProject(_projectGroupName);

        //if (project != null)
        //{
        //    if (!string.IsNullOrWhiteSpace(project.SeedProjectFilePath))
        //        projectGroupSubMenuSet.AddMenuItem(
        //            new GitSubMenuCommand(_logger, _parametersManager, _projectGroupName, EGitCol.ScaffoldSeed),
        //            "Git ScaffoldSeeder projects");

        //    //დასაშვები ინსტრუმენტების არჩევა
        //    projectGroupSubMenuSet.AddMenuItem(new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _projectGroupName),
        //        "Select Allow tools...");

        //    foreach (var tool in ToolCommandFabric.ToolsByProjects.Intersect(project.AllowToolsList))
        //        projectGroupSubMenuSet.AddMenuItem(
        //            new ToolTaskCliMenuCommand(_logger, tool, _projectGroupName, null, _parametersManager), tool.ToString());
        //}

        //var serverInfoCruder = new ServerInfoCruder(_logger, _parametersManager, _projectGroupName);

        ////ახალი სერვერის ინფორმაციის შექმნა
        //var newItemCommand = new NewItemCommand(serverInfoCruder, serverInfoCruder.CrudNamePlural,
        //    $"New {serverInfoCruder.CrudName}");
        //projectGroupSubMenuSet.AddMenuItem(newItemCommand);

        ////სერვერების ჩამონათვალი
        //if (project?.ServerInfos != null)
        //    foreach (var kvp in project.ServerInfos.OrderBy(o => o.Value.GetItemKey()))
        //        projectGroupSubMenuSet.AddMenuItem(
        //            new ServerInfoSubMenuCommand(_logger, _parametersManager, _projectGroupName, kvp.Key),
        //            kvp.Value.GetItemKey());


        //პროექტების ჩამონათვალი
        foreach (var kvp in parameters.Projects
                     .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                                 _projectGroupName)
                     .OrderBy(o => o.Key))
            projectGroupSubMenuSet.AddMenuItem(new ProjectSubMenuCliMenuCommand(_logger, _parametersManager, kvp.Key),
                kvp.Key);


        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        projectGroupSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCliMenuCommand(null, null),
            key.Length);

        return projectGroupSubMenuSet;
    }

    protected override string GetStatus()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        return parameters.Projects
            .Count(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
            .ToString();
    }
}