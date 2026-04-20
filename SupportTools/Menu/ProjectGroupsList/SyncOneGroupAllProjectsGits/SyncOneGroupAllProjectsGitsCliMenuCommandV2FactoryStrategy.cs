using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

// ReSharper disable once UnusedType.Global
public class SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy(
        ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy> logger, MenuParameters menuParameters)
    {
        _logger = logger;
        _menuParameters = menuParameters;
    }

    public string MenuCommandName => SyncOneGroupAllProjectsGitsCliMenuCommandV2.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new SyncOneGroupAllProjectsGitsCliMenuCommandV2(_logger, parametersManager,
            _menuParameters.ProjectGroupName);
    }
}
