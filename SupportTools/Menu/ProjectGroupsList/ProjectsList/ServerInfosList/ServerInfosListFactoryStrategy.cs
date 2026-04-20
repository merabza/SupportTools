using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ServerInfosList;

// ReSharper disable once UnusedType.Global
public class ServerInfosListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServerInfosListFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ServerInfosListFactoryStrategy(IApplication app, IServiceProvider serviceProvider,
        ILogger<ServerInfosListFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        MenuParameters menuParameters, IParametersManager parametersManager)
    {
        _app = app;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public string MenuCommandListName => nameof(ServerInfoSubMenuCliMenuCommand);

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_menuParameters.ProjectName);

        if (project?.ServerInfos != null)
        {
            return project.ServerInfos.OrderBy(o => o.Value.GetItemKey()).Select(kvp =>
                    new ServerInfoSubMenuCliMenuCommand(_app.AppName, _logger, _httpClientFactory,
                        kvp.Value.GetItemKey(),
                        _parametersManager, _menuParameters.ProjectName, kvp.Key, _serviceProvider))
                .Cast<CliMenuCommand>()
                .ToList();
        }

        return [];
    }
}
