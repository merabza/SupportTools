using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.SyncAllProjectsAllGits;

// ReSharper disable once ClassNeverInstantiated.Global
public class SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    public SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy(
        ILogger<SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy> logger, IParametersManager parametersManager)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy);

    public CliMenuCommand CreateMenuCommand()
    {
        var parametersManager1 = (ParametersManager)_parametersManager;
        return new SyncAllProjectsAllGitsCliMenuCommandV2(_logger, parametersManager1);
    }
}
