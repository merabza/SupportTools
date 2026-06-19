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

    public override CliMenuSet GetSubMenu()
    {
        _menuParameters.ProjectGroupName = _projectGroupName;
        return CliMenuSetFactory.CreateMenuSet(_projectGroupName,
            MenuData.ProjectGroupSubMenuCommandFactoryStrategyNames, _serviceProvider);
    }

    protected override string GetStatus()
    {
        List<string> projectNames = GetGroupProjectNames();

        //სანამ ამ ჯგუფის არცერთი პროექტი არ შემოწმებულა, ისე ვტოვებთ, როგორც აქამდე იყო - მხოლოდ რაოდენობა
        if (!projectNames.Any(IsProjectChecked))
        {
            return projectNames.Count.ToString(CultureInfo.InvariantCulture);
        }

        //ჯგუფში შემავალი პროექტების სტატუსები და მათი რაოდენობები
        return string.Join(", ", BuildBreakdown(projectNames).Select(b =>
            $"{ProjectBuildCheckStatusView.GetName(b.Status)}: {b.Count.ToString(CultureInfo.InvariantCulture)}"));
    }

    protected override IReadOnlyList<StatusColorPart>? BuildStatusColorParts()
    {
        List<string> projectNames = GetGroupProjectNames();

        //სანამ ამ ჯგუფის არცერთი პროექტი არ შემოწმებულა, ფერადი სტატუსები არ გვინდა (რჩება ჩვეულებრივი რაოდენობა)
        if (!projectNames.Any(IsProjectChecked))
        {
            return null;
        }

        return BuildBreakdown(projectNames).Select(b => new StatusColorPart(
                $"{ProjectBuildCheckStatusView.GetName(b.Status)}: {b.Count.ToString(CultureInfo.InvariantCulture)}",
                ProjectBuildCheckStatusView.GetColor(b.Status)))
            .ToList();
    }

    private List<string> GetGroupProjectNames()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        return parameters.Projects
            .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
            .Select(x => x.Key).ToList();
    }

    private List<(EProjectBuildCheckStatus? Status, int Count)> BuildBreakdown(IEnumerable<string> projectNames)
    {
        return projectNames
            .GroupBy(GetProjectStatus)
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, g.Count()))
            .ToList();
    }

    private bool IsProjectChecked(string projectName)
    {
        return _menuParameters.ProjectBuildCheckStatuses.ContainsKey(projectName);
    }

    private EProjectBuildCheckStatus? GetProjectStatus(string projectName)
    {
        return _menuParameters.ProjectBuildCheckStatuses.TryGetValue(projectName, out EProjectBuildCheckStatus status)
            ? status
            : null;
    }
}
