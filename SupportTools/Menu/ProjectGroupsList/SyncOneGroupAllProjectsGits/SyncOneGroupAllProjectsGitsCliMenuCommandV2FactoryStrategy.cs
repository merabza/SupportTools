using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy(
    ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneGroupAllProjectsGitsCliMenuCommandV2(logger, parametersManager,
            menuParameters.ProjectGroupName);
    }
}
