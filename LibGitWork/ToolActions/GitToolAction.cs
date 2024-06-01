using LibToolActions;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace LibGitWork.ToolActions;

public class GitToolAction : ToolAction
{
    protected GitToolAction(ILogger logger, string actionName, IMessagesDataManager? messagesDataManager,
        string? userName, bool useConsole = false) : base(logger, actionName, messagesDataManager, userName, useConsole)
    {
    }
}