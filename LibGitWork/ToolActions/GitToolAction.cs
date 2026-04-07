using Microsoft.Extensions.Logging;
using SystemTools.BackgroundTasks;
using SystemTools.SystemToolsShared;

namespace LibGitWork.ToolActions;

public /*open*/ class GitToolAction : ToolAction
{
    protected GitToolAction(ILogger? logger, string actionName, IMessagesDataManager? messagesDataManager,
        string? userName, bool useConsole = false) : base(logger, actionName, messagesDataManager, userName, useConsole)
    {
    }
}
