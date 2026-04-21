using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;

// ReSharper disable once UnusedType.Global
public class GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        ILogger<GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new GitSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _menuParameters.ProjectName, EGitCol.ScaffoldSeed);
    }
}
