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
        return string.Join(", ", BuildStatusParts(projectNames).Select(p => p.Text));
    }

    protected override IReadOnlyList<StatusColorPart>? BuildStatusColorParts()
    {
        List<string> projectNames = GetGroupProjectNames();

        //სანამ ამ ჯგუფის არცერთი პროექტი არ შემოწმებულა, ფერადი სტატუსები არ გვინდა (რჩება ჩვეულებრივი რაოდენობა)
        return projectNames.Any(IsProjectChecked) ? BuildStatusParts(projectNames) : null;
    }

    //ჯგუფის სტატუსის ნაწილები: სტატუსების რაოდენობები და შემოწმებული პროექტების შეცდომების/გაფრთხილებების ჯამი.
    //GetStatus-ის ტექსტი ზუსტად ამ ნაწილების ტექსტია (მენიუ ნაწილებს ", "-ით აერთებს)
    private List<StatusColorPart> BuildStatusParts(List<string> projectNames)
    {
        List<StatusColorPart> parts =
        [
            .. BuildBreakdown(projectNames).Select(b => new StatusColorPart(
                $"{ProjectBuildCheckStatusView.GetName(b.Status)}: {b.Count.ToString(CultureInfo.InvariantCulture)}",
                ProjectBuildCheckStatusView.GetColor(b.Status)))
        ];

        int errorCount = projectNames.Sum(projectName => GetProjectResult(projectName)?.ErrorCount ?? 0);
        int warningCount = projectNames.Sum(projectName => GetProjectResult(projectName)?.WarningCount ?? 0);
        ProjectBuildCheckStatusView.AddCountParts(parts, errorCount, warningCount);
        return parts;
    }

    private List<string> GetGroupProjectNames()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;
        return
        [
            .. parameters.Projects
                .Where(x => SupportToolsParameters.FixProjectGroupName(x.Value.ProjectGroupName) == _projectGroupName)
                .Select(x => x.Key)
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
