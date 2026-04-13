using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Menu.CreateCruderNewItem;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CreateNewProject;

// ReSharper disable once UnusedMember.Global
public class NewProjectCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NewProjectCliMenuCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NewProjectCliMenuCommandFactoryStrategy(ILogger<NewProjectCliMenuCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IApplication application)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _application = application;
    }

    public string MenuCommandName => $"New {ProjectCruder.MenuCommandName}";

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        var projectCruder = new ProjectCruder(_application.Name, _logger, _httpClientFactory, parametersManager,
            parameters.Projects);

        //ახალი პროექტის შექმნა
        return new NewItemCliMenuCommand(projectCruder, projectCruder.CrudNamePlural, $"New {projectCruder.CrudName}");
    }
}
