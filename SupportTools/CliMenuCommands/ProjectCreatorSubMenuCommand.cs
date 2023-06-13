using System;
using System.Linq;
using CliMenu;
using CliParameters.MenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportTools.Cruders;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectCreatorSubMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;


    public ProjectCreatorSubMenuCommand(ILogger logger, ParametersManager parametersManager) : base("Project Creator")
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }


    public override CliMenuSet GetSubmenu()
    {
        CliMenuSet projectCreatorSubMenuSet = new("Project Creator");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var appProjectCreatorAllParameters = parameters.AppProjectCreatorAllParameters;
        if (appProjectCreatorAllParameters is null)
        {
            parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters();
            appProjectCreatorAllParameters = parameters.AppProjectCreatorAllParameters;
        }

        if (appProjectCreatorAllParameters is not null)
        {
            AppProjectCreatorParametersEditor appProjectCreatorParametersEditor = new(_logger,
                appProjectCreatorAllParameters, _parametersManager, _parametersManager);

            appProjectCreatorParametersEditor.FillDetailsSubMenu(projectCreatorSubMenuSet);
        }

        var creatorClassCreatorToolCommand = new CreatorClassCreatorCliMenuCommand(_logger);
        projectCreatorSubMenuSet.AddMenuItem(creatorClassCreatorToolCommand);

        var binaryFileCreatorClassCreatorCliMenuCommand = new BinaryFileCreatorClassCreatorCliMenuCommand(_logger);
        projectCreatorSubMenuSet.AddMenuItem(binaryFileCreatorClassCreatorCliMenuCommand);

        var createAllTemplateTestProjectsToolCommand =
            new CreateAllTemplateTestProjectsCliMenuCommand(_logger, _parametersManager);
        projectCreatorSubMenuSet.AddMenuItem(createAllTemplateTestProjectsToolCommand);

        TemplateCruder projectCreatorTemplateCruder = new(_logger, _parametersManager);

        //ახალი პროექტის შექმნა
        NewItemCommand newItemCommand = new(projectCreatorTemplateCruder, projectCreatorTemplateCruder.CrudNamePlural,
            $"New {projectCreatorTemplateCruder.CrudName}");
        projectCreatorSubMenuSet.AddMenuItem(newItemCommand);

        //პროექტების ჩამონათვალი
        if (appProjectCreatorAllParameters is not null)
            foreach (var kvp in appProjectCreatorAllParameters.Templates.OrderBy(o =>
                         o.Key))
                projectCreatorSubMenuSet.AddMenuItem(new TemplateSubMenuCommand(_logger, _parametersManager, kvp.Key),
                    kvp.Key);

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        projectCreatorSubMenuSet.AddMenuItem(key, "Exit to Main menu", new ExitToMainMenuCommand(null, null),
            key.Length);

        return projectCreatorSubMenuSet;
    }
}