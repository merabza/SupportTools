using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.CliMenuCommands;
using AppCliTools.CliTools.Services.RecentCommands;
using ParametersManagement.LibParameters;

namespace SupportTools.Menu.RecentCommandsList;

// ReSharper disable once UnusedType.Global
public class RecentCommandsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly IRecentCommandsService _recentCommandsService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RecentCommandsListFactoryStrategy(IRecentCommandsService recentCommandsService)
    {
        _recentCommandsService = recentCommandsService;
    }

    public string StrategyName => nameof(RecentCommandsListFactoryStrategy);

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        return _recentCommandsService.GetRecentCommands().Cast<CliMenuCommand>().ToList();
    }
}
