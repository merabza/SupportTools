using CliMenu;
using LibGitWork.ToolActions;
using LibParameters;
using Microsoft.Extensions.Logging;
using SupportToolsData;
using System.Threading;

namespace LibGitWork.CliMenuCommands;

public sealed class GitSyncCliMenuCommand : CliMenuCommand
{
    private readonly EGitCol _gitCol;
    private readonly string _gitProjectName;
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;
    private readonly string _projectName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public GitSyncCliMenuCommand(ILogger logger, ParametersManager parametersManager, string projectName,
        string gitProjectName, EGitCol gitCol) : base("Sync", null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    protected override string GetActionDescription()
    {
        return $"This process will Sync git {_gitProjectName}";
    }


    protected override void RunAction()
    {
        MenuAction = EMenuAction.Reload;

        var gitSyncToolAction =
            GitSyncToolAction.Create(_logger, _parametersManager, _projectName, _gitCol, _gitProjectName);
        gitSyncToolAction?.Run(CancellationToken.None).Wait();
    }
}