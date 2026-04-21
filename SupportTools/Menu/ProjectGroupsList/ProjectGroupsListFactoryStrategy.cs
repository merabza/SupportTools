using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectGroupsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupsListFactoryStrategy(IServiceProvider serviceProvider, MenuParameters menuParameters,
        IParametersManager parametersManager)
    {
        _serviceProvider = serviceProvider;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
    }

    public string StrategyName => nameof(ProjectGroupsListFactoryStrategy);

    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        return parameters.Projects.Select(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName))
            .Distinct().OrderBy(x => x)
            .Select(projectGroupName => new ProjectGroupSubMenuCliMenuCommand(_serviceProvider, _parametersManager,
                projectGroupName, _menuParameters)).Cast<CliMenuCommand>().ToList();
    }
}
