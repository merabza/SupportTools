using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.SyncOneProjectAllGitsWithScaffoldSeeders;

// ReSharper disable once UnusedType.Global
public class SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> _logger;
    private readonly MenuParameters _menuParameters;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy(
        ILogger<SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy> logger,
        MenuParameters menuParameters)
    {
        _logger = logger;
        _menuParameters = menuParameters;
    }

    public string StrategyName => nameof(SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2FactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        return new SyncOneProjectAllGitsWithScaffoldSeedersCliMenuCommandV2(_logger, parametersManager,
            _menuParameters.ProjectGroupName);
    }
}
