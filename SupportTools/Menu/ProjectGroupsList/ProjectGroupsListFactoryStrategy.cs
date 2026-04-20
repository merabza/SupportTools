using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList;

// ReSharper disable once UnusedType.Global
public class ProjectGroupsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupsListFactoryStrategy(IServiceProvider serviceProvider, MenuParameters menuParameters)
    {
        _serviceProvider = serviceProvider;
        _menuParameters = menuParameters;
    }

    public string MenuCommandListName => nameof(ProjectGroupSubMenuCliMenuCommand);

    public List<CliMenuCommand> CreateMenuCommandsList(IParametersManager parametersManager)
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        return parameters.Projects.Select(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName))
            .Distinct().OrderBy(x => x)
            .Select(projectGroupName =>
                new ProjectGroupSubMenuCliMenuCommand(_serviceProvider, parametersManager, projectGroupName,
                    _menuParameters)).Cast<CliMenuCommand>().ToList();
    }
}
