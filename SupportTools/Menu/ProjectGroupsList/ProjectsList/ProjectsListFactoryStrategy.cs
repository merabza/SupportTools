using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList;

// ReSharper disable once UnusedType.Global
public class ProjectsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectsListFactoryStrategy(IServiceProvider serviceProvider, MenuParameters menuParameters)
    {
        _serviceProvider = serviceProvider;
        _menuParameters = menuParameters;
    }

    public string MenuCommandListName => nameof(ProjectSubMenuCliMenuCommand);

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        //პროექტების ჩამონათვალი
        return parameters.Projects
            .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                        _menuParameters.ProjectGroupName).OrderBy(o => o.Key).Select(kvp =>
                new ProjectSubMenuCliMenuCommand(_serviceProvider, parametersManager, kvp.Key, _menuParameters))
            .Cast<CliMenuCommand>().ToList();
    }
}
