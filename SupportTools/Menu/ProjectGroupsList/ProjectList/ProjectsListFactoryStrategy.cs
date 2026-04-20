using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectList;

// ReSharper disable once UnusedType.Global
public class ProjectsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProjectsListFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsListFactoryStrategy(IApplication app, IServiceProvider serviceProvider,
        ILogger<ProjectsListFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        MenuParameters menuParameters)
    {
        _app = app;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _menuParameters = menuParameters;
    }

    public string MenuCommandListName => nameof(ProjectSubMenuCliMenuCommand);

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        //პროექტების ჩამონათვალი
        return parameters.Projects
            .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                        _menuParameters.ProjectGroupName).OrderBy(o => o.Key).Select(kvp =>
                new ProjectSubMenuCliMenuCommand(_app.AppName, _serviceProvider, _logger, _httpClientFactory,
                    parametersManager, kvp.Key)).Cast<CliMenuCommand>().ToList();
    }
}
