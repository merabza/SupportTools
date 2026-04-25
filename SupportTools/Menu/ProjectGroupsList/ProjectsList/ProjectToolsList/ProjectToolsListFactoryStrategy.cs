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
public class ProjectToolsListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly ILogger<ProjectToolsListFactoryStrategy> _logger;
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectToolsListFactoryStrategy(ILogger<ProjectToolsListFactoryStrategy> logger,
        SupportToolsMenuParameters menuParameters, IParametersManager parametersManager,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _menuParameters = menuParameters;
        _parametersManager = parametersManager;
        _serviceProvider = serviceProvider;
    }

    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_menuParameters.ProjectName);

        if (project != null)
        {
            return Enum.GetValues<EProjectTools>().Intersect(project.AllowToolsList)
                .OrderBy(x => x.GetProjectToolName())
                .Select(tool =>
                    new ProjectToolTaskCliMenuCommand(_serviceProvider, tool, _menuParameters.ProjectName,
                        _parametersManager)).Cast<CliMenuCommand>().ToList();
        }

        _logger.LogWarning("Project not found: {ProjectName}", _menuParameters.ProjectName);
        return [];
    }
}
