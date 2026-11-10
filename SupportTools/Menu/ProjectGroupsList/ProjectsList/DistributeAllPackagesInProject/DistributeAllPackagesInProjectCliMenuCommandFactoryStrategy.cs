using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.DistributeAllPackagesInProject;

// ReSharper disable once ClassNeverInstantiated.Global
public class DistributeAllPackagesInProjectCliMenuCommandFactoryStrategy(
    IApplication app,
    ILogger<DistributeAllPackagesInProjectCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new DistributeAllPackagesInProjectCliMenuCommand(logger, app.AppName, httpClientFactory,
            parametersManager, menuParameters, menuParameters.ProjectName);
    }
}
