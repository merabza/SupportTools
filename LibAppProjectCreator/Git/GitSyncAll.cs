using System.Collections.Generic;
using System.Linq;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Domain;

namespace LibAppProjectCreator.Git;

public sealed class GitSyncAll : ToolAction
{
    private readonly IEnumerable<GitDataDomain> _gitDataModel;
    private readonly string _gitsFolder;

    public GitSyncAll(ILogger logger, string gitsFolder, IEnumerable<GitDataDomain> gitDataModel) : base(logger,
        "Git Sync All", null, null)
    {
        _gitsFolder = gitsFolder;
        _gitDataModel = gitDataModel;
    }

    protected override bool CheckValidate()
    {
        return true;
    }

    protected override bool RunAction()
    {
        string? commitMessage = null;
        foreach (var gitData in _gitDataModel.OrderBy(x => x.GitProjectFolderName))
        {
            var gitSync = new GitSync(Logger, _gitsFolder, gitData, commitMessage, commitMessage == null);
            gitSync.Run();
            commitMessage = gitSync.UsedCommitMessage;
        }

        return true;
    }
}