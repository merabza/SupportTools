using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectGroupsListFactoryStrategy(
    IServiceProvider serviceProvider,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;

        return
        [
            .. parameters.Projects.Select(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName))
                .Distinct().OrderBy(x => x).Select(projectGroupName =>
                    new ProjectGroupSubMenuCliMenuCommand(serviceProvider, parametersManager, projectGroupName,
                        menuParameters))
        ];
    }
}
