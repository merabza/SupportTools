using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;

// ReSharper disable once ClassNeverInstantiated.Global
public class GitSubMenuCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    ILogger<GitSubMenuCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new GitSubMenuCliMenuCommand(logger, httpClientFactory, parametersManager, menuParameters.ProjectName,
            EGitCol.Main);
    }
}
