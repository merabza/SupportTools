using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.GitScaffoldSeederProjects;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;
using SupportToolsData;
using SupportToolsData.Models;
using SystemTools.SystemToolsShared;

namespace SupportTools.Menu.ProjectGroupsList.ProjectsList;

public sealed class ProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectSubMenuCliMenuCommand(IServiceProvider serviceProvider, IParametersManager parametersManager,
        string projectName, SupportToolsMenuParameters menuParameters) : base(projectName, EMenuAction.LoadSubMenu)
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
        //პაკეტის დაბილდვის ბრძანება ჩანს მხოლოდ პაკეტის ტიპის პროექტებისთვის
        if (project is null || project.ProjectType != EProjectType.IsPackage)
        {
            excludeList.Add(nameof(BuildPackageCliMenuCommandFactoryStrategy));
        }

        if (project is null || string.IsNullOrWhiteSpace(project.ScaffoldSeederProjectName))
        {
            excludeList.Add(nameof(GitScaffoldSeederProjectsCliMenuCommandFactoryStrategy));
        }

        if (!SystemStat.IsWindows() || string.IsNullOrWhiteSpace(project?.SolutionFileName))
        {
            excludeList.Add(nameof(OpenByVisualStudioCliMenuCommandFactoryStrategy));
        }

        _menuParameters.ProjectName = _projectName;
        return CliMenuSetFactory.CreateMenuSet(_projectName,
            MenuData.ProjectSubMenuCommandFactoryStrategyNames
                .Where(menuCommandName => !excludeList.Contains(menuCommandName)).ToList(), _serviceProvider);
    }

    protected override string? GetStatus()
    {
        //სტატუსი ჩანს მხოლოდ მაშინ, თუ ეს პროექტი უკვე შემოწმდა
        return GetProjectStatus()?.ToString();
    }

    protected override IReadOnlyList<StatusColorPart>? BuildStatusColorParts()
    {
        EProjectBuildCheckStatus? status = GetProjectStatus();
        if (status is null)
        {
            return null;
        }

        return [new StatusColorPart(status.Value.ToString(), ProjectBuildCheckStatusView.GetColor(status))];
    }

    private EProjectBuildCheckStatus? GetProjectStatus()
    {
        return _menuParameters.ProjectBuildCheckStatuses.TryGetValue(_projectName, out EProjectBuildCheckStatus status)
            ? status
            : null;
    }
}
