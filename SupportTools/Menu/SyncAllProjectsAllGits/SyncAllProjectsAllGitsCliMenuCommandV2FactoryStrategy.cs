using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SyncAllProjectsAllGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy(
    ILogger<SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy> logger,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new SyncAllProjectsAllGitsCliMenuCommandV2(logger, parametersManager1);
    }
}
