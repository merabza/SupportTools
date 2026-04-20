using System;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Menu.CreateCruderNewItem;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Cruders;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CreateNewServerInfo;

// ReSharper disable once UnusedMember.Global
public class CreateNewServerInfoFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CreateNewServerInfoFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CreateNewServerInfoFactoryStrategy(ILogger<CreateNewServerInfoFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IApplication application, IServiceProvider serviceProvider,
        IParametersManager parametersManager, MenuParameters menuParameters)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _application = application;
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
        _menuParameters = menuParameters;
    }

    public string MenuCommandName => nameof(CreateNewServerInfoFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var serverInfoCruder = ServerInfoCruder.Create(_application.AppName, _serviceProvider, _logger,
            _httpClientFactory, _parametersManager, _menuParameters.ProjectName);

        //ახალი სერვერის ინფორმაციის შექმნა
        return new NewItemCliMenuCommand(serverInfoCruder, serverInfoCruder.CrudNamePlural,
            $"New {serverInfoCruder.CrudName}");
    }
}
