using System;
using System.Linq;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSubMenuCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName) :
        base(projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
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
        projectSubMenuSet.AddMenuItem(editCommand, "Edit All fields in sequence");

        projectCruder.FillDetailsSubMenu(projectSubMenuSet, _projectName);

        projectSubMenuSet.AddMenuItem(new GitSubMenuCliMenuCommand(_logger, _parametersManager, _projectName, EGitCol.Main),
            "Git");

        var project = parameters.GetProject(_projectName);

        if (project != null)
        {
            if (!string.IsNullOrWhiteSpace(project.SeedProjectFilePath))
                projectSubMenuSet.AddMenuItem(
                    new GitSubMenuCliMenuCommand(_logger, _parametersManager, _projectName, EGitCol.ScaffoldSeed),
                    "Git ScaffoldSeeder projects");

            //დასაშვები ინსტრუმენტების არჩევა
            projectSubMenuSet.AddMenuItem(new SelectProjectAllowToolsCliMenuCommand(_parametersManager, _projectName),
                "Select Allow tools...");

            foreach (var tool in ToolCommandFabric.ToolsByProjects.Intersect(project.AllowToolsList))
                projectSubMenuSet.AddMenuItem(
                    new ToolTaskCliMenuCommand(_logger, tool, _projectName, null, _parametersManager), tool.ToString());
        }

        var serverInfoCruder = new ServerInfoCruder(_logger, _parametersManager, _projectName);

        //ახალი სერვერის ინფორმაციის შექმნა
        var newItemCommand = new NewItemCliMenuCommand(serverInfoCruder, serverInfoCruder.CrudNamePlural,
            $"New {serverInfoCruder.CrudName}");
        projectSubMenuSet.AddMenuItem(newItemCommand);

        //სერვერების ჩამონათვალი
        if (project?.ServerInfos != null)
            foreach (var kvp in project.ServerInfos.OrderBy(o => o.Value.GetItemKey()))
                projectSubMenuSet.AddMenuItem(
                    new ServerInfoSubMenuCliMenuCommand(_logger, _parametersManager, _projectName, kvp.Key),
                    kvp.Value.GetItemKey());

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        projectSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCliMenuCommand(null, null), key.Length);

        return projectSubMenuSet;
    }
}