using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;

// ReSharper disable once ClassNeverInstantiated.Global
public class GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy> _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy(SupportToolsMenuParameters menuParameters,
        ILogger<GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new GitSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _menuParameters.ProjectName, EGitCol.ScaffoldSeed);
    }
}
