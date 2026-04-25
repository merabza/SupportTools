using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy> _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy(
        ILogger<SyncOneGroupAllProjectsGitsCliMenuCommandV2FactoryStrategy> logger,
        SupportToolsMenuParameters menuParameters, IParametersManager parametersManager)
    {
        _logger = logger;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneGroupAllProjectsGitsCliMenuCommandV2(_logger, _parametersManager,
            _menuParameters.ProjectGroupName);
    }
}
