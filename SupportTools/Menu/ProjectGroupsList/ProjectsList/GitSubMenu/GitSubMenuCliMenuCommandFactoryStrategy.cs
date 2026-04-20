using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;

// ReSharper disable once UnusedType.Global
public class GitSubMenuCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitSubMenuCliMenuCommandFactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSubMenuCliMenuCommandFactoryStrategy(MenuParameters menuParameters,
        ILogger<GitSubMenuCliMenuCommandFactoryStrategy> logger, IHttpClientFactory httpClientFactory,
        IParametersManager parametersManager)
    {
        _menuParameters = menuParameters;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    public string MenuCommandName => nameof(GitSubMenuCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new GitSubMenuCliMenuCommand(_logger, _httpClientFactory, _parametersManager,
            _menuParameters.ProjectName, EGitCol.Main);
    }
}
