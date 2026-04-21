using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.Services.RecentCommands;

namespace SupportTools.Menu.RecentCommandsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class RecentCommandsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly IRecentCommandsService _recentCommandsService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RecentCommandsListFactoryStrategy(IRecentCommandsService recentCommandsService)
    {
        _recentCommandsService = recentCommandsService;
    }

    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        return _recentCommandsService.GetRecentCommands().Cast<CliMenuCommand>().ToList();
    }
}
