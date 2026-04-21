using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CreateNewProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class CreateNewProjectFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CreateNewProjectFactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateNewProjectFactoryStrategy(ILogger<CreateNewProjectFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IApplication application, IParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _application = application;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        var projectCruder = new ProjectCruder(_application.AppName, _logger, _httpClientFactory, _parametersManager,
            parameters.Projects);

        //ახალი პროექტის შექმნა
        return new NewItemCliMenuCommand(projectCruder, projectCruder.CrudNamePlural, $"New {projectCruder.CrudName}");
    }
}
