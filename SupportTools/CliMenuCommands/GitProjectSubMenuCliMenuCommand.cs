using System;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibGitData;
using LibGitWork.CliMenuCommands;
using LibParameters;
using Microsoft.Extensions.Logging;

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
        string gitProjectName, EGitCol gitCol) : base(gitProjectName, EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    public override CliMenuSet GetSubMenu()
    {
        //git-ის პროექტის მენიუს შექმნა
        var gitProjectSubMenuSet = new CliMenuSet(_gitProjectName);

        //გიტის პროექტის წაშლა ამ პროექტიდან
        var deleteGitProjectCommand =
            new DeleteGitProjectCliMenuCommand(_parametersManager, _projectName, _gitProjectName, _gitCol);
        gitProjectSubMenuSet.AddMenuItem(deleteGitProjectCommand);

        gitProjectSubMenuSet.AddMenuItem(new GitSyncCliMenuCommand(_logger, _parametersManager, _projectName,
            _gitProjectName, _gitCol));

        //პროექტის მენიუში დაბრუნება
        var key = ConsoleKey.Escape.Value().ToLower();
        gitProjectSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to Git menu", null), key.Length);

        return gitProjectSubMenuSet;
    }
}