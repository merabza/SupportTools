using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.CreateProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectCreatorSubMenuCliMenuCommandFactoryStrategy(
    ILogger<ProjectCreatorSubMenuCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IApplication application,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new ProjectCreatorSubMenuCliMenuCommand(application, logger, httpClientFactory, parametersManager);
    }
}
