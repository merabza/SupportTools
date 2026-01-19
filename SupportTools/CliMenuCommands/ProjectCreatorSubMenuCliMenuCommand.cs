using System;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class ProjectCreatorSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectCreatorSubMenuCliMenuCommand(ILogger logger, IHttpClientFactory httpClientFactory,
        ParametersManager parametersManager) : base("Project Creator", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var projectCreatorSubMenuSet = new CliMenuSet("Project Creator");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        var appProjectCreatorAllParameters = parameters.AppProjectCreatorAllParameters;
        if (appProjectCreatorAllParameters is null)
        {
            parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters();
            appProjectCreatorAllParameters = parameters.AppProjectCreatorAllParameters;
        }

        if (appProjectCreatorAllParameters is not null)
        {
            var appProjectCreatorParametersEditor = new AppProjectCreatorParametersEditor(_logger, _httpClientFactory,
                appProjectCreatorAllParameters, _parametersManager, _parametersManager);

            appProjectCreatorParametersEditor.FillDetailsSubMenu(projectCreatorSubMenuSet);
        }

        var creatorClassCreatorToolCommand = new CreatorClassCreatorCliMenuCommand(_logger);
        projectCreatorSubMenuSet.AddMenuItem(creatorClassCreatorToolCommand);

        var binaryFileCreatorClassCreatorCliMenuCommand = new BinaryFileCreatorClassCreatorCliMenuCommand(_logger);
        projectCreatorSubMenuSet.AddMenuItem(binaryFileCreatorClassCreatorCliMenuCommand);

        var createAllTemplateTestProjectsToolCommand =
            new CreateAllTemplateTestProjectsCliMenuCommand(_logger, _httpClientFactory, _parametersManager);
        projectCreatorSubMenuSet.AddMenuItem(createAllTemplateTestProjectsToolCommand);

        var projectCreatorTemplateCruder = TemplateCruder.Create(_logger, _parametersManager);

        //ახალი პროექტის შექმნა
        var newItemCommand = new NewItemCliMenuCommand(projectCreatorTemplateCruder,
            projectCreatorTemplateCruder.CrudNamePlural, $"New {projectCreatorTemplateCruder.CrudName}");
        projectCreatorSubMenuSet.AddMenuItem(newItemCommand);

        //პროექტების ჩამონათვალი
        if (appProjectCreatorAllParameters is not null)
            foreach (var kvp in appProjectCreatorAllParameters.Templates.OrderBy(o => o.Key))
                projectCreatorSubMenuSet.AddMenuItem(
                    new TemplateSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager, kvp.Key));

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        projectCreatorSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null),
            key.Length);

        return projectCreatorSubMenuSet;
    }
}