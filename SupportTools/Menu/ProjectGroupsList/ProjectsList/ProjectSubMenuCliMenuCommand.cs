using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.BuildPackage;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CheckPackageSolution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.CloneProjectToThisComputer;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.OpenByVisualStudio;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.PackageDistribution;
using SupportTools.Menu.ProjectGroupsList.ProjectsList.SaveEditorConfigAsNewTemplate;
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

    public override CliMenuSet? GetSubMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);

        List<string> excludeList = [];
        //პაკეტის შემოწმების, დაბილდვისა და გავრცელების ბრძანებები ჩანს მხოლოდ პაკეტის ტიპის პროექტებისთვის
        if (project is null || project.ProjectType != EProjectType.IsPackage)
        {
            excludeList.Add(nameof(CheckPackageSolutionCliMenuCommandFactoryStrategy));
            excludeList.Add(nameof(BuildPackageCliMenuCommandFactoryStrategy));
            excludeList.Add(nameof(PackageDistributionCliMenuCommandFactoryStrategy));
        }

        if (!SystemStat.IsWindows() || string.IsNullOrWhiteSpace(project?.SolutionFileName))
        {
            excludeList.Add(nameof(OpenByVisualStudioCliMenuCommandFactoryStrategy));
        }

        //პროექტის .editorconfig ფაილი solution ფაილის ფოლდერში მდებარეობს
        if (project?.EditorConfigFileName() is null)
        {
            excludeList.Add(nameof(SaveEditorConfigAsNewTemplateCliMenuCommandFactoryStrategy));
        }

        //პროექტის ამ კომპიუტერზე ჩამოტანა ჩანს მხოლოდ remote პროექტებისთვის (D3)
        if (project is null || ProjectPresence.IsLocal(project))
        {
            excludeList.Add(nameof(CloneProjectToThisComputerCliMenuCommandFactoryStrategy));
        }

        _menuParameters.ProjectName = _projectName;
        return CliMenuSetFactory.CreateMenuSet(_projectName, [
            .. MenuData.ProjectSubMenuCommandFactoryStrategyNames.Where(menuCommandName =>
                !excludeList.Contains(menuCommandName))
        ], _serviceProvider);
    }

    protected override string GetStatus()
    {
        return ProjectPresenceView.GetText(BuildStatusParts());
    }

    protected override IReadOnlyList<StatusColorPart> BuildStatusColorParts()
    {
        return BuildStatusParts();
    }

    //სტატუსი: local ან remote (D3), მერე build-ის შედეგი, თუ ეს პროექტი უკვე შემოწმდა
    private List<StatusColorPart> BuildStatusParts()
    {
        List<StatusColorPart> parts = [ProjectPresenceView.CreatePart(IsLocal())];

        ProjectBuildCheckResult? result = GetProjectResult();
        if (result is not null)
        {
            parts.AddRange(ProjectBuildCheckStatusView.BuildParts(result));
        }

        return parts;
    }

    private bool IsLocal()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        ProjectModel? project = parameters.GetProject(_projectName);
        return project is not null && ProjectPresence.IsLocal(project);
    }

    private ProjectBuildCheckResult? GetProjectResult()
    {
        return _menuParameters.ProjectBuildCheckResults.GetValueOrDefault(_projectName);
    }
}
