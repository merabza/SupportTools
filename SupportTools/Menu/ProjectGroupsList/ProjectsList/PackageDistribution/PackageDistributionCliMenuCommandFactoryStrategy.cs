using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;

// ReSharper disable once ClassNeverInstantiated.Global
public class PackageDistributionCliMenuCommandFactoryStrategy(
    ILogger<PackageDistributionCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new PackageDistributionCliMenuCommand(logger, httpClientFactory, (ParametersManager)parametersManager,
            menuParameters.ProjectName);
    }
}
