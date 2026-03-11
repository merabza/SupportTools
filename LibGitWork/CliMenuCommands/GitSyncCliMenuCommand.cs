using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using LibGitData;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

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
        string gitProjectName, EGitCol gitCol) : base("Sync", EMenuAction.Reload, EMenuAction.Reload, null, true)
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _projectName = projectName;
        _gitProjectName = gitProjectName;
        _gitCol = gitCol;
    }

    protected override ValueTask<string?> GetActionDescription(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>($"This process will Sync git {_gitProjectName}");
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var gitSyncToolAction =
            GitSyncToolAction.Create(_logger, _parametersManager, _projectName, _gitCol, _gitProjectName, true);
        if (gitSyncToolAction is null)
        {
            return false;
        }

        return await gitSyncToolAction.Run(cancellationToken);
    }
}
