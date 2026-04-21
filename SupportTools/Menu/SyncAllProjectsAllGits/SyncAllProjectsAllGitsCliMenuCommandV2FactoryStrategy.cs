using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.SyncOneGroupAllProjectsGits;

namespace SupportTools.Menu.SyncAllProjectsAllGits;

// ReSharper disable once UnusedType.Global
public class SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ILogger<SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy(
        ILogger<SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy> logger)
    {
        _logger = logger;
    }

    public string StrategyName => nameof(SyncAllProjectsAllGitsCliMenuCommandV2FactoryStrategy);

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var parametersManager1 = (ParametersManager)parametersManager;
        return new SyncAllProjectsAllGitsCliMenuCommandV2(_logger, parametersManager1);
    }
}
