using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy(
        ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> logger,
        SupportToolsMenuParameters menuParameters, IParametersManager parametersManager)
    {
        _logger = logger;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(_logger, _parametersManager,
            _menuParameters.ProjectGroupName);
    }
}
