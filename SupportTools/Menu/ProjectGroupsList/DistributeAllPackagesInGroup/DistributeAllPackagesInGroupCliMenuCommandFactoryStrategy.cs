using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.DistributeAllPackagesInGroup;

// ReSharper disable once ClassNeverInstantiated.Global
public class DistributeAllPackagesInGroupCliMenuCommandFactoryStrategy(
    IApplication app,
    ILogger<DistributeAllPackagesInGroupCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new DistributeAllPackagesInGroupCliMenuCommand(logger, app.AppName, httpClientFactory,
            parametersManager, menuParameters, menuParameters.ProjectGroupName);
    }
}
