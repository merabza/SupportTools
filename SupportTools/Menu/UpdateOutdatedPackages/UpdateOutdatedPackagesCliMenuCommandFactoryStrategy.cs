using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.UpdateOutdatedPackages;

// ReSharper disable once ClassNeverInstantiated.Global
public class UpdateOutdatedPackagesCliMenuCommandFactoryStrategy(
    ILogger<UpdateOutdatedPackagesCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = parametersManager;
        return new UpdateOutdatedPackagesCliMenuCommand(logger, httpClientFactory, parametersManager1);
    }
}
