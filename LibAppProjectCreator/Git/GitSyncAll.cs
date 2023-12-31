using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using LibToolActions;
using Microsoft.Extensions.Logging;
using SupportToolsData.Domain;
// ReSharper disable ConvertToPrimaryConstructor

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

    protected override async Task<bool> RunAction(CancellationToken cancellationToken)
    {
        string? commitMessage = null;
        foreach (var gitData in _gitDataModel.OrderBy(x => x.GitProjectFolderName))
        {
            var gitSync = new GitSync(Logger, _gitsFolder, gitData, commitMessage, commitMessage == null);
            await gitSync.Run(cancellationToken);
            commitMessage = gitSync.UsedCommitMessage;
        }

        return true;
    }
}