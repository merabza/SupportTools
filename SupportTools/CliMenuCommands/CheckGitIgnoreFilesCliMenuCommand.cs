using System.Threading;
using CliMenu;
using LibGitWork;
using LibGitWork.ToolActions;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

namespace SupportTools.CliMenuCommands;

public sealed class CheckGitIgnoreFilesCliMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public CheckGitIgnoreFilesCliMenuCommand(ILogger logger, IParametersManager parametersManager) : base(
        CheckGitIgnoreFilesToolAction.ActionName, EMenuAction.Reload)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        var updateGitProjectsToolAction = new CheckGitIgnoreFilesToolAction(_logger, _parametersManager, true);
        return updateGitProjectsToolAction.Run(CancellationToken.None).Result;
    }

    protected override string GetStatus()
    {
        MenuAction = EMenuAction.Reload;
        var wrongGitignoreFilesListCreator = new WrongGitignoreFilesListCreator(_logger, _parametersManager, true);
        var wrongGitIgnoreFilesList = wrongGitignoreFilesListCreator.Create();

        return wrongGitIgnoreFilesList.Count.ToString();
    }
}