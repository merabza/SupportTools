using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliMenu.CliMenuCommands;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using ParametersManagement.LibParameters;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class FindGitProjectsWithoutMatchingProjectsCliMenuCommand : CliMenuCommand
{
    private readonly Cruder _gitCruder;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FindGitProjectsWithoutMatchingProjectsCliMenuCommand(Cruder gitCruder,
        IParametersManager parametersManager) : base("Find Git Projects Without Matching Projects",
        EMenuAction.LoadSubMenu)
    {
        _gitCruder = gitCruder;
        _parametersManager = parametersManager;
    }

    public override CliMenuSet GetSubMenu()
    {
        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        //ვპოულობთ გიტის პროექტებს, რომლებსაც არ მოეძებნება შესაბამისი სახელის SupportTools-ის პროექტი
        List<string> unmatchedNames =
        [
            .. parameters.Gits.Keys.Where(x => !parameters.Projects.ContainsKey(x))
                .OrderBy(x => x, StringComparer.Ordinal)
        ];

        var menuSet = new CliMenuSet("Git Projects Without Matching Projects");

        //არჩევისას იხსნება შესაბამისი გიტის პროექტის ქვემენიუ
        foreach (string unmatchedName in unmatchedNames)
        {
            menuSet.AddMenuItem(new ItemSubMenuCliMenuCommand(_gitCruder, unmatchedName, _gitCruder.CrudNamePlural));
        }

        menuSet.AddEscapeCommand(new ExitToMainMenuCliMenuCommand("Refresh Gits Menu", null));

        return menuSet;
    }
}
