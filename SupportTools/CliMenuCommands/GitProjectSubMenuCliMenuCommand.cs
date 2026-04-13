using AppCliTools.CliMenu;
using LibGitData;
using LibGitWork.CliMenuCommands;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class GitProjectSubMenuCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitProjectSubMenuCliMenuCommand(ILogger logger, IParametersManager parametersManager, string projectName,
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
        gitProjectSubMenuSet.AddEscapeCommand("Exit to Git menu");

        return gitProjectSubMenuSet;
    }
}
