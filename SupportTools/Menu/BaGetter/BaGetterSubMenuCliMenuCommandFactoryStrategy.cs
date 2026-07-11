using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.BaGetter;

// ReSharper disable once ClassNeverInstantiated.Global
public class BaGetterSubMenuCliMenuCommandFactoryStrategy(
    ILogger<BaGetterSubMenuCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new BaGetterSubMenuCliMenuCommand(logger, httpClientFactory, parametersManager);
    }
}
