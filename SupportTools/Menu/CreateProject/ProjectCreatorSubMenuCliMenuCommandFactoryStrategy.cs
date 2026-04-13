using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CreateProject;

// ReSharper disable once UnusedType.Global
public class ProjectCreatorSubMenuCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IApplication _application;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProjectCreatorSubMenuCliMenuCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectCreatorSubMenuCliMenuCommandFactoryStrategy(
        ILogger<ProjectCreatorSubMenuCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IApplication application)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _application = application;
    }

    public string MenuCommandName => ProjectCreatorSubMenuCliMenuCommand.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new ProjectCreatorSubMenuCliMenuCommand(_application.Name, _logger, _httpClientFactory,
            parametersManager);
    }
}
