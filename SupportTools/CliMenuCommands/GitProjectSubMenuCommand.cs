using System;
using CliMenu;
using CliParameters.MenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;

namespace SupportTools.CliMenuCommands;

public sealed class GitProjectSubMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    public GitProjectSubMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        string gitProjectName, EGitCol gitCol) : base(projectName)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    protected override void RunAction()
    {
        MenuAction = EMenuAction.LoadSubMenu;
    }


    public override CliMenuSet GetSubmenu()
    {
        //git-ის პროექტის მენიუს შექმნა
        CliMenuSet gitProjectSubMenuSet = new(_gitProjectName);

        //გიტის პროექტის წაშლა ამ პროექტიდან
        DeleteGitProjectCliMenuCommand deleteGitProjectCommand =
            new(_parametersManager, _projectName, _gitProjectName, _gitCol);
        gitProjectSubMenuSet.AddMenuItem(deleteGitProjectCommand);

        //პროექტის პარამეტრი
        //GitCruder gitCruder = new(_logger, _parametersManager);
        //EditItemAllFieldsInSequenceCommand editCommand = new(gitCruder, _gitProjectName);
        //gitProjectSubMenuSet.AddMenuItem(editCommand, "Edit All fields in sequence");

        //gitCruder.FillDetailsSubMenu(gitProjectSubMenuSet, _gitProjectName);

        gitProjectSubMenuSet.AddMenuItem(new SyncGitCliMenuCommand(_logger, _parametersManager, _projectName,
            _gitProjectName, _gitCol));

        //პროექტის მენიუში დაბრუნება
        var key = ConsoleKey.Escape.Value().ToLower();
        gitProjectSubMenuSet.AddMenuItem(key, "Exit to Git menu", new ExitToMainMenuCommand(null, null), key.Length);

        return gitProjectSubMenuSet;
    }
}