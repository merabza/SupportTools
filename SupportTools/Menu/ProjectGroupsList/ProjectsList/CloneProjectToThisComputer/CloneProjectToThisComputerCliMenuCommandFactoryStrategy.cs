using System.Net.Http;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;

// ReSharper disable once ClassNeverInstantiated.Global
public class CloneProjectToThisComputerCliMenuCommandFactoryStrategy(
    ILogger<CloneProjectToThisComputerCliMenuCommandFactoryStrategy> logger,
    IHttpClientFactory httpClientFactory,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new CloneProjectToThisComputerCliMenuCommand(logger, httpClientFactory, parametersManager,
            menuParameters.ProjectName);
    }
}
