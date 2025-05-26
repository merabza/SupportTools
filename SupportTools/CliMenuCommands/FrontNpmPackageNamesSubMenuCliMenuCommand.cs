using System;
using System.Linq;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData.Models;

namespace SupportTools.CliMenuCommands;

public sealed class FrontNpmPackageNamesSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FrontNpmPackageNamesSubMenuCliMenuCommand(ILogger logger, ParametersManager parametersManager,
        string projectName) : base("Front Npm Package Names", EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
    }

    public override CliMenuSet GetSubMenu()
    {
        var gitSubMenuSet = new CliMenuSet("Front Npm Package Names");

        var parameters = (SupportToolsParameters)_parametersManager.Parameters;

        ////მენიუს ელემენტი, რომლის საშუალებითაც შესაძლებელია პროექტში გიტის ჩაგდება
        var newGitCommand = new NewFrontNpmPackageNameCliMenuCommand(_logger, _parametersManager, _projectName);
        gitSubMenuSet.AddMenuItem(newGitCommand);

        var npmPackageNames = parameters.GetNpmPackageNames(_projectName);

        foreach (var gitProjectName in npmPackageNames.OrderBy(o => o))
            gitSubMenuSet.AddMenuItem(new NpmPackageInProjectSubMenuCliMenuCommand(_parametersManager, _projectName,
                gitProjectName));

        var addAllPossibleNpmPackageNames = new AddAllPossibleNpmPackageNamesFromStpToProjectCliMenuCommand(_parametersManager, _projectName);
        gitSubMenuSet.AddMenuItem(addAllPossibleNpmPackageNames);

        //მთავარ მენიუში გასვლა
        var key = ConsoleKey.Escape.Value().ToLower();
        gitSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);

        return gitSubMenuSet;
    }
}