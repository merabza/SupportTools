using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using SupportToolsData;
using SupportToolsData.Models;

namespace SupportTools.Menu.ProjectGroupsList;

public sealed class ProjectGroupSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly SupportToolsMenuParameters _menuParameters;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectGroupName;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ProjectGroupSubMenuCliMenuCommand(IServiceProvider serviceProvider, IParametersManager parametersManager,
        string projectGroupName, SupportToolsMenuParameters menuParameters) : base(projectGroupName,
        EMenuAction.LoadSubMenu)
    {
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
        _projectGroupName = projectGroupName;
        _menuParameters = menuParameters;
    }

    public override CliMenuSet? GetSubMenu()
    {
        _menuParameters.ProjectGroupName = _projectGroupName;
        return CliMenuSetFactory.CreateMenuSet(_projectGroupName,
            MenuData.ProjectGroupSubMenuCommandFactoryStrategyNames, _serviceProvider);
    }

    protected override string GetStatus()
    {
        return ProjectPresenceView.GetText(BuildStatusParts());
    }

    protected override IReadOnlyList<StatusColorPart> BuildStatusColorParts()
    {
        return BuildStatusParts();
    }

    //ჯგუფის სტატუსის ნაწილები: ჯერ local/remote რაოდენობები (D3), მერე, თუ ჯგუფის რომელიმე პროექტი უკვე შემოწმდა,
    //სტატუსების რაოდენობები და შემოწმებული პროექტების შეცდომების/გაფრთხილებების ჯამი.
    //GetStatus-ის ტექსტი ზუსტად ამ ნაწილების ტექსტია (მენიუ ნაწილებს ", "-ით აერთებს)
    private List<StatusColorPart> BuildStatusParts()
    {
        List<KeyValuePair<string, ProjectModel>> groupProjects = GetGroupProjects();
        List<StatusColorPart> parts = ProjectPresenceView.CreateCountParts(groupProjects.Select(x => x.Value));

        List<string> projectNames = [.. groupProjects.Select(x => x.Key)];
        if (!projectNames.Any(IsProjectChecked))
        {
            return parts;
        }

        parts.AddRange(
        [
            .. BuildBreakdown(projectNames).Select(b => new StatusColorPart(
                $"{ProjectBuildCheckStatusView.GetName(b.Status)}: {b.Count.ToString(CultureInfo.InvariantCulture)}",
                ProjectBuildCheckStatusView.GetColor(b.Status)))
        ]);

        int errorCount = projectNames.Sum(projectName => GetProjectResult(projectName)?.ErrorCount ?? 0);
        int warningCount = projectNames.Sum(projectName => GetProjectResult(projectName)?.WarningCount ?? 0);
        ProjectBuildCheckStatusView.AddCountParts(parts, errorCount, warningCount);
        return parts;
    }

    //HideRemoteOnlyProjects-ისას მხოლოდ ამ კომპიუტერზე არსებული პროექტები ითვლება, ისევე როგორც პროექტების სიაში
    private List<KeyValuePair<string, ProjectModel>> GetGroupProjects()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        return
        [
            .. ProjectPresenceView.GetVisibleProjects(parameters).Where(x =>
                SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
        ];
    }

    private List<(EProjectBuildCheckStatus? Status, int Count)> BuildBreakdown(List<string> projectNames)
    {
        return [.. projectNames.GroupBy(GetProjectStatus).OrderBy(g => g.Key).Select(g => (g.Key, g.Count()))];
    }

    private bool IsProjectChecked(string projectName)
    {
        return _menuParameters.ProjectBuildCheckResults.ContainsKey(projectName);
    }

    private ProjectBuildCheckResult? GetProjectResult(string projectName)
    {
        return _menuParameters.ProjectBuildCheckResults.GetValueOrDefault(projectName);
    }

    private EProjectBuildCheckStatus? GetProjectStatus(string projectName)
    {
        return GetProjectResult(projectName)?.Status;
    }
}
