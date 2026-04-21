using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;

    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy(
        ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> logger,
        MenuParameters menuParameters, IParametersManager parametersManager)
    {
        _logger = logger;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy);

    public CliMenuCommand CreateMenuCommand()
    {
        return new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(_logger, _parametersManager,
            _menuParameters.ProjectGroupName);
    }
}
