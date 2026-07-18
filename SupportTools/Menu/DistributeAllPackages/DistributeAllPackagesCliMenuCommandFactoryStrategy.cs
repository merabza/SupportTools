using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.DistributeAllPackages;

// ReSharper disable once ClassNeverInstantiated.Global
public class DistributeAllPackagesCliMenuCommandFactoryStrategy(
    IApplication app,
    ILogger<DistributeAllPackagesCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new DistributeAllPackagesCliMenuCommand(logger, app.AppName, httpClientFactory,
            (ParametersManager)parametersManager, menuParameters);
    }
}
