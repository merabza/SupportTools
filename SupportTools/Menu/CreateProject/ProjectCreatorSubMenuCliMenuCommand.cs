using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.CliMenuCommands;
using SupportTools.Cruders;
using SupportTools.ParametersEditors;
using SupportToolsData.Models;

namespace SupportTools.Menu.CreateProject;

public sealed class ProjectCreatorSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    public static string MenuCommandName => "Project Creator";

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectCreatorSubMenuCliMenuCommand(string appName, ILogger logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager) : base(MenuCommandName, EMenuAction.LoadSubMenu)
    {
        _appName = appName;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var projectCreatorSubMenuSet = new CliMenuSet("Project Creator");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        AppProjectCreatorAllParameters? appProjectCreatorAllParameters = parameters.AppProjectCreatorAllParameters;
        if (appProjectCreatorAllParameters is null)
        {
            parameters.AppProjectCreatorAllParameters = new AppProjectCreatorAllParameters();
            appProjectCreatorAllParameters = parameters.AppProjectCreatorAllParameters;
        }

        var appProjectCreatorParametersEditor = new AppProjectCreatorParametersEditor(_appName, _logger,
            _httpClientFactory, appProjectCreatorAllParameters, _parametersManager, _parametersManager);

        appProjectCreatorParametersEditor.FillDetailsSubMenu(projectCreatorSubMenuSet);

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
        foreach (KeyValuePair<string, TemplateModel> kvp in
                 appProjectCreatorAllParameters.Templates.OrderBy(o => o.Key))
        {
            projectCreatorSubMenuSet.AddMenuItem(
                new TemplateSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager, kvp.Key));
        }

        //მთავარ მენიუში გასვლა
        projectCreatorSubMenuSet.AddEscapeCommand();

        return projectCreatorSubMenuSet;
    }
}
