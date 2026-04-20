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
public class CreateNewProjectFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CreateNewProjectFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateNewProjectFactoryStrategy(ILogger<CreateNewProjectFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IApplication application)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _application = application;
    }

    public string MenuCommandName => nameof(CreateNewProjectFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        var projectCruder = new ProjectCruder(_application.AppName, _logger, _httpClientFactory, parametersManager,
            parameters.Projects);

        //ახალი პროექტის შექმნა
        return new NewItemCliMenuCommand(projectCruder, projectCruder.CrudNamePlural, $"New {projectCruder.CrudName}");
    }
}
