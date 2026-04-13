using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList;

// ReSharper disable once UnusedType.Global
public class ProjectGroupsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProjectGroupsListFactoryStrategy> _logger;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupsListFactoryStrategy(IApplication app, IServiceProvider serviceProvider,
        ILogger<ProjectGroupsListFactoryStrategy> logger, IHttpClientFactory httpClientFactory)
    {
        _app = app;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string MenuCommandListName => ProjectGroupSubMenuCliMenuCommand.MenuCommandListName;

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        return parameters.Projects.Select(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName))
            .Distinct().OrderBy(x => x).Select(projectGroupName => new ProjectGroupSubMenuCliMenuCommand(_app.Name,
                _serviceProvider, _logger, _httpClientFactory, parametersManager, projectGroupName))
            .Cast<CliMenuCommand>().ToList();
    }
}
