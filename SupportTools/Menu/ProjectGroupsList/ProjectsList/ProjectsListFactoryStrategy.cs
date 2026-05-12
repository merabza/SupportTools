using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectsListFactoryStrategy(
    IServiceProvider serviceProvider,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        //პროექტების ჩამონათვალი
        return parameters.Projects
            .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) ==
                        menuParameters.ProjectGroupName).OrderBy(o => o.Key).Select(kvp =>
                new ProjectSubMenuCliMenuCommand(serviceProvider, parametersManager, kvp.Key, menuParameters))
            .Cast<CliMenuCommand>().ToList();
    }
}
