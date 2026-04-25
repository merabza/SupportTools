using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsListFactoryStrategy(IServiceProvider serviceProvider, SupportToolsMenuParameters menuParameters,
        IParametersManager parametersManager)
    {
        _serviceProvider = serviceProvider;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        //პროექტების ჩამონათვალი
        return parameters.Projects
            .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                        _menuParameters.ProjectGroupName).OrderBy(o => o.Key).Select(kvp =>
                new ProjectSubMenuCliMenuCommand(_serviceProvider, _parametersManager, kvp.Key, _menuParameters))
            .Cast<CliMenuCommand>().ToList();
    }
}
