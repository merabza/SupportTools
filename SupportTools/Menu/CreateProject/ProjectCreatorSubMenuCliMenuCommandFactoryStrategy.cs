using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CreateProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectCreatorSubMenuCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProjectCreatorSubMenuCliMenuCommandFactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectCreatorSubMenuCliMenuCommandFactoryStrategy(
        ILogger<ProjectCreatorSubMenuCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IApplication application, IParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _application = application;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new ProjectCreatorSubMenuCliMenuCommand(_application.AppName, _logger, _httpClientFactory,
            _parametersManager);
    }
}
