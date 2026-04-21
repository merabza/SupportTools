using System.Collections.Generic;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectParametersList;

// ReSharper disable once UnusedType.Global
public class ProjectParametersListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProjectParametersListFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectParametersListFactoryStrategy(ILogger<ProjectParametersListFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, MenuParameters menuParameters, IParametersManager parametersManager,
        IApplication application)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
        _application = application;
    }

    public string StrategyName => nameof(ProjectParametersListFactoryStrategy);

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        var projectCruder = ProjectCruder.Create(_application.AppName, _logger, _httpClientFactory, _parametersManager);

        return projectCruder.GetDetailsSubMenu(_menuParameters.ProjectName);
    }
}
