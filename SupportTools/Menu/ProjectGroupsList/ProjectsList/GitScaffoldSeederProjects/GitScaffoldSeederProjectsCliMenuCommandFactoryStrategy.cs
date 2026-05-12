using System.Net.Http;
using AppCliTools.CliMenu;
using LibGitData;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitSubMenu;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;

// ReSharper disable once ClassNeverInstantiated.Global
public class GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy(
    SupportToolsMenuParameters menuParameters,
    ILogger<GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new GitSubMenuCliMenuCommand(logger, httpClientFactory, parametersManager, menuParameters.ProjectName,
            EGitCol.ScaffoldSeed);
    }
}
