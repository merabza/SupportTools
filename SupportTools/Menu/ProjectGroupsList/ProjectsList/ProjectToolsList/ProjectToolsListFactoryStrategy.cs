using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList.ProjectToolsList;

// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectToolsListFactoryStrategy(
    ILogger<ProjectToolsListFactoryStrategy> logger,
    SupportToolsMenuParameters menuParameters,
    IParametersManager parametersManager,
    IServiceProvider serviceProvider) : IMenuCommandListFactoryStrategy
{
    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(menuParameters.ProjectName);

        if (project != null)
        {
            return Enum.GetValues<EProjectTools>().Intersect(project.AllowToolsList)
                .OrderBy(x => x.GetProjectToolName())
                .Select(tool =>
                    new ProjectToolTaskCliMenuCommand(serviceProvider, tool, menuParameters.ProjectName,
                        parametersManager)).Cast<CliMenuCommand>().ToList();
        }

        logger.LogWarning("Project not found: {ProjectName}", menuParameters.ProjectName);
        return [];
    }
}
