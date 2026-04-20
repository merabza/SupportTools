using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList;

public sealed class ProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly MenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSubMenuCliMenuCommand(IServiceProvider serviceProvider, IParametersManager parametersManager,
        string projectName, MenuParameters menuParameters) : base(projectName, EMenuAction.LoadSubMenu)
    {
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _menuParameters = menuParameters;
    }

    public override CliMenuSet GetSubMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        List<string> excludeList = [];
        if (project is null || string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
        {
            excludeList.Add(nameof(GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy));
        }

        _menuParameters.ProjectName = _projectName;
        return CliMenuSetFactory.CreateMenuSet(_projectName,
            MenuData.ProjectSubMenuCommandNames.Where(menuCommandName => !excludeList.Contains(menuCommandName))
                .ToList(), _serviceProvider, _parametersManager);
    }
}
