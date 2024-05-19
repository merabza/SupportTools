using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitData;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;
using System;

namespace SupportTools.CliMenuCommands;

public sealed class GitProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectSubMenuCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        string gitProjectName, EGitCol gitCol) : base(projectName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
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

        gitProjectSubMenuSet.AddMenuItem(new GitSyncCliMenuCommand(_logger, _parametersManager, _projectName,
            _gitProjectName, _gitCol));

        //პროექტის მენიუში დაბრუნება
        var key = ConsoleKey.Escape.Value().ToLower();
        gitProjectSubMenuSet.AddMenuItem(key, "Exit to Git menu", new ExitToMainMenuCliMenuCommand(null, null),
            key.Length);

        return gitProjectSubMenuSet;
    }
}