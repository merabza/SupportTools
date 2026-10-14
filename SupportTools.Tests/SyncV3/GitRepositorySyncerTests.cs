using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGitWork.SyncV3;
using Xunit;

namespace SupportTools.Tests.SyncV3;

//runs the real git over a throwaway bare remote (a local path, so no network is needed). "Upstream" is a clone that
//plays another computer: its pushes are the changes the folders under test have not seen yet
public sealed class GitRepositorySyncerTests : IDisposable
{
    private const string GitName = "RepoA";

    private readonly GitFolderCommands _commands = new(null);
    private readonly string _remote;
    private readonly List<string> _steps = [];
    private readonly string _tempFolder = Directory.CreateTempSubdirectory("SupportToolsTests_").FullName;
    private readonly string _upstream;

    public GitRepositorySyncerTests()
    {
        _remote = GitTestRepositories.CreateRemote(_tempFolder, GitName,
            new Dictionary<string, string> { ["Readme.txt"] = GitName, [".gitignore"] = "bin/" });
        _upstream = Clone("Upstream");
    }

    public void Dispose()
    {
        GitTestRepositories.DeleteFolder(_tempFolder);
    }

    //the remote moved on, but SupportToolsServer still knows the commit of the folder: the folder is not fetched
    [Fact]
    public void Run_WhenTheFolderIsAtTheKnownHead_DoesNotAskTheRemote()
    {
        // Arrange
        string folder = Clone("A");
        string knownCommit = Head(folder);
        PushNewCommit("b.txt");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(new GitHead("main", knownCommit), folder).Outcomes);

        // Assert
        Assert.True(outcome.WithoutRequest);
        Assert.Equal(knownCommit, Head(folder));
        Assert.Empty(_steps);
    }

    [Fact]
    public void Run_WhenTheFolderIsBehindTheKnownHead_FetchesAndFastForwards()
    {
        // Arrange
        string folder = Clone("A");
        string newCommit = PushNewCommit("b.txt");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(new GitHead("main", newCommit), folder).Outcomes);

        // Assert
        Assert.Equal((true, true, false, null), (outcome.Fetched, outcome.Pulled, outcome.Pushed, outcome.Error));
        Assert.Equal(newCommit, Head(folder));
        Assert.Equal(["fetch", "merge"], _steps);
    }

    //without a known head the first folder fetches; the head it saw serves the next folder of the same git
    [Fact]
    public void Run_WhenNoHeadIsKnown_FetchesOnceForTwoFoldersAtTheSameCommit()
    {
        // Arrange
        string folderA = Clone("A");
        string folderB = Clone("B");

        // Act
        GitRepositorySyncer sut = Run(null, folderA, folderB);

        // Assert
        Assert.Equal([(true, false), (false, true)], sut.Outcomes.Select(x => (x.Fetched, x.WithoutRequest)));
    }

    [Fact]
    public void Run_WhenTheFolderHasChanges_CommitsAndPushes()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.Equal((true, true, null), (outcome.Committed, outcome.Pushed, outcome.Error));
        Assert.Equal("Sync message", GitTestRepositories.RunGit("-C", _remote, "log", "-1", "--format=%s"));
    }

    //the second folder merges the push of the first one, so the first one is behind after the second push and is
    //synced once more
    [Fact]
    public void Run_WhenTwoFoldersOfTheGitHaveChanges_EndsWithBothAtTheSameCommit()
    {
        // Arrange
        string folderA = Clone("A");
        string folderB = Clone("B");
        File.WriteAllText(Path.Combine(folderA, "a.txt"), "a");
        File.WriteAllText(Path.Combine(folderB, "b.txt"), "b");

        // Act
        GitRepositorySyncer sut = Run(null, folderA, folderB);

        // Assert
        Assert.All(sut.Outcomes, x => Assert.Equal((true, true, null), (x.Committed, x.Pushed, x.Error)));
        string remoteHead = GitTestRepositories.RunGit("-C", _remote, "rev-parse", "main");
        Assert.Equal(remoteHead, Head(folderA));
        Assert.Equal(remoteHead, Head(folderB));
        Assert.True(File.Exists(Path.Combine(folderA, "b.txt")));
    }

    //the remote moves between the fetch and the push (here by the pre-push hook, once): the push is rejected, so the
    //folder fetches again, merges by pull (pull.rebase=true) and pushes again
    [Fact]
    public void Run_WhenThePushIsRejected_FetchesMergesAndPushesAgain()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_upstream, "u.txt"), "u");
        GitTestRepositories.RunGit("-C", _upstream, "add", "--all");
        GitTestRepositories.RunGit("-C", _upstream, "commit", "-m", "Upstream commit");
        string marker = Path.Combine(_tempFolder, "hook-ran");
        File.WriteAllText(Path.Combine(folder, ".git", "hooks", "pre-push"),
            $"#!/bin/sh\nif [ ! -f \"{ShellPath(marker)}\" ]; then\n  touch \"{ShellPath(marker)}\"\n" +
            "  unset GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE GIT_PREFIX\n" +
            $"  git -C \"{ShellPath(_upstream)}\" push -q origin HEAD:main\nfi\nexit 0\n");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.True(File.Exists(marker));
        Assert.Equal((true, true, true, null), (outcome.Committed, outcome.Pulled, outcome.Pushed, outcome.Error));
        Assert.Equal(["Sync message", "Upstream commit", "Initial"],
            GitTestRepositories.RunGit("-C", _remote, "log", "--format=%s", "main").Split('\n'));
        Assert.Equal(["commit", "status", "fetch", "push", "fetch", "pull", "push"], _steps);
    }

    [Fact]
    public void Run_WhenTheFolderIsMissing_ClonesIt()
    {
        // Arrange
        string folder = Path.Combine(_tempFolder, "Projects", "New", GitName);

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.True(outcome.Cloned);
        Assert.Equal(GitTestRepositories.RunGit("-C", _remote, "rev-parse", "main"), Head(folder));
        Assert.Equal(["clone"], _steps);
    }

    //the changed folder goes first, so the clean one gets its push with one fetch and no second pass
    [Fact]
    public void Run_WhenOneFolderHasChanges_SyncsItFirst()
    {
        // Arrange
        string cleanFolder = Clone("Clean");
        string changedFolder = Clone("Changed");
        File.WriteAllText(Path.Combine(changedFolder, "a.txt"), "a");
        List<(string Folder, string Step)> steps = [];
        List<GitFolderInspection> inspections =
        [
            GitFolderInspection.Inspect(_commands, Folder(cleanFolder)),
            GitFolderInspection.Inspect(_commands, Folder(changedFolder))
        ];
        var sut = new GitRepositorySyncer(_commands, inspections, null,
            new Dictionary<string, string> { [changedFolder] = "Sync message" },
            (folder, step) => steps.Add((Path.GetFileName(folder.FolderPath), step)));

        // Act
        sut.Run();

        // Assert
        Assert.Equal(
        [
            ("Changed", "commit"), ("Changed", "status"), ("Changed", "fetch"), ("Changed", "push"),
            ("Clean", "fetch"), ("Clean", "merge")
        ], steps);
    }

    //a folder that failed before the last push is not taken up again by the second pass
    [Fact]
    public void Run_WhenAFolderFailedBeforeTheLastPush_LeavesIt()
    {
        // Arrange
        string failingFolder = Clone("Failing");
        string pushingFolder = Clone("Pushing");
        File.WriteAllText(Path.Combine(failingFolder, "a.txt"), "a");
        File.WriteAllText(Path.Combine(pushingFolder, "b.txt"), "b");
        List<GitFolderInspection> inspections =
        [
            GitFolderInspection.Inspect(_commands, Folder(failingFolder)),
            GitFolderInspection.Inspect(_commands, Folder(pushingFolder))
        ];
        File.WriteAllText(Path.Combine(failingFolder, ".git", "index.lock"), string.Empty);
        List<(string Folder, string Step)> steps = [];
        var sut = new GitRepositorySyncer(_commands, inspections, null,
            new Dictionary<string, string> { [failingFolder] = "Message A", [pushingFolder] = "Message B" },
            (folder, step) => steps.Add((Path.GetFileName(folder.FolderPath), step)));

        // Act
        sut.Run();

        // Assert
        Assert.Equal([("Failing", "commit")], steps.Where(x => x.Folder == "Failing"));
        Assert.True(sut.Outcomes[1].Pushed);
    }

    //a post-merge hook puts HEAD back after every merge, so the state never settles: the loop stops after ten steps
    [Fact]
    public void Run_WhenTheStateNeverSettles_StopsAfterTenSteps()
    {
        // Arrange
        string folder = Clone("A");
        string newCommit = PushNewCommit("b.txt");
        File.WriteAllText(Path.Combine(folder, ".git", "hooks", "post-merge"),
            "#!/bin/sh\ngit reset -q --hard ORIG_HEAD\n");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(new GitHead("main", newCommit), folder).Outcomes);

        // Assert
        Assert.Equal("the sync did not finish in 10 steps", outcome.Error);
        Assert.Equal(["fetch", .. Enumerable.Repeat("merge", 10)], _steps);
    }

    //the folder pulls in both passes (a merge of the other computer's commit, then a fast-forward to the push of the
    //second folder): it is still reported as pulled
    [Fact]
    public void Run_WhenAFolderPullsInBothPasses_ReportsItAsPulled()
    {
        // Arrange
        string folderA = Clone("A");
        string folderB = Clone("B");
        PushNewCommit("u.txt");
        File.WriteAllText(Path.Combine(folderA, "a.txt"), "a");
        File.WriteAllText(Path.Combine(folderB, "b.txt"), "b");

        // Act
        GitRepositorySyncer sut = Run(null, folderA, folderB);

        // Assert
        Assert.All(sut.Outcomes, x => Assert.Equal((true, true, null), (x.Pulled, x.Pushed, x.Error)));
        Assert.Equal(Head(folderB), Head(folderA));
    }

    //the remote moves twice: before the sync and between the fetch and the push (the pre-push hook, once). The folder
    //pulls twice and pushes; it is reported as pulled
    [Fact]
    public void Run_WhenTheFolderPullsTwiceForOnePush_ReportsItAsPulled()
    {
        // Arrange
        string folder = Clone("A");
        PushNewCommit("u0.txt");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_upstream, "u1.txt"), "u1");
        GitTestRepositories.RunGit("-C", _upstream, "add", "--all");
        GitTestRepositories.RunGit("-C", _upstream, "commit", "-q", "-m", "Second upstream commit");
        string marker = Path.Combine(_tempFolder, "hook-ran");
        File.WriteAllText(Path.Combine(folder, ".git", "hooks", "pre-push"),
            $"#!/bin/sh\nif [ ! -f \"{ShellPath(marker)}\" ]; then\n  touch \"{ShellPath(marker)}\"\n" +
            "  unset GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE GIT_PREFIX\n" +
            $"  git -C \"{ShellPath(_upstream)}\" push -q origin HEAD:main\nfi\nexit 0\n");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.Equal((true, true, null), (outcome.Pulled, outcome.Pushed, outcome.Error));
        Assert.Equal(["commit", "status", "fetch", "pull", "push", "fetch", "pull", "push"], _steps);
    }

    //a commit is made in the first folder while the second one pushes: the second pass merges and pushes it, so the
    //first folder pushed twice and is reported as pushed
    [Fact]
    public void Run_WhenAFolderPushesInBothPasses_ReportsItAsPushed()
    {
        // Arrange
        string folderA = Clone("A");
        string folderB = Clone("B");
        File.WriteAllText(Path.Combine(folderA, "a.txt"), "a");
        File.WriteAllText(Path.Combine(folderB, "b.txt"), "b");
        List<GitFolderInspection> inspections =
        [
            GitFolderInspection.Inspect(_commands, Folder(folderA)),
            GitFolderInspection.Inspect(_commands, Folder(folderB))
        ];
        bool committed = false;
        var sut = new GitRepositorySyncer(_commands, inspections, null,
            new Dictionary<string, string> { [folderA] = "Message A", [folderB] = "Message B" }, (folder, step) =>
            {
                if (committed || folder.FolderPath != folderB || step != "push")
                {
                    return;
                }

                committed = true;
                File.WriteAllText(Path.Combine(folderA, "a2.txt"), "a2");
                GitTestRepositories.RunGit("-C", folderA, "add", "--all");
                GitTestRepositories.RunGit("-C", folderA, "commit", "-q", "-m", "Second commit of A");
            });

        // Act
        sut.Run();

        // Assert
        Assert.Equal((true, null), (sut.Outcomes[0].Pushed, sut.Outcomes[0].Error));
        Assert.Equal("Second commit of A", GitTestRepositories.RunGit("-C", _remote, "log", "-1", "--format=%s"));
    }

    //a repository without a commit has no HEAD to compare
    [Fact]
    public void Run_WhenTheFolderHasNoCommit_ReportsIt()
    {
        // Arrange
        string folder = GitTestRepositories.InitRepository(Path.Combine(_tempFolder, "Empty"));
        GitTestRepositories.RunGit("-C", folder, "remote", "add", "origin", _remote);
        var inspection = new GitFolderInspection(Folder(folder), EGitFolderInspectionKind.Ready,
            new GitFolderStatus(null, null, true, false), null);
        var sut = new GitRepositorySyncer(_commands, [inspection], null, new Dictionary<string, string>(),
            (_, step) => _steps.Add(step));

        // Act
        sut.Run();

        // Assert
        Assert.StartsWith("git rev-parse @ failed", Assert.Single(sut.Outcomes).Error, StringComparison.Ordinal);
    }

    //a branch without a common ancestor with its upstream (an orphan branch) cannot be merged or pushed
    [Fact]
    public void Run_WhenTheBranchHasNoCommonAncestorWithItsUpstream_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        GitTestRepositories.RunGit("-C", folder, "checkout", "-q", "--orphan", "other");
        GitTestRepositories.RunGit("-C", folder, "commit", "-q", "-m", "Orphan");
        GitTestRepositories.RunGit("-C", folder, "branch", "-q", "--set-upstream-to=origin/main");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.StartsWith("git merge-base @ @{u} failed", outcome.Error, StringComparison.Ordinal);
    }

    //git status fails right after the commit (status.showUntrackedFiles is set to a wrong value then)
    [Fact]
    public void Run_WhenTheStatusAfterTheCommitFails_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        var sut = new GitRepositorySyncer(_commands, [GitFolderInspection.Inspect(_commands, Folder(folder))], null,
            new Dictionary<string, string> { [folder] = "Sync message" }, (_, step) =>
            {
                _steps.Add(step);
                if (step == "status")
                {
                    GitTestRepositories.RunGit("-C", folder, "config", "status.showUntrackedFiles", "bogus");
                }
            });

        // Act
        sut.Run();

        // Assert
        GitFolderSyncOutcome outcome = Assert.Single(sut.Outcomes);
        Assert.True(outcome.Committed);
        Assert.StartsWith("git status --porcelain=v2 --branch --untracked-files=normal failed", outcome.Error,
            StringComparison.Ordinal);
        Assert.Equal(["commit", "status"], _steps);
    }

    //HEAD is at the known head, but the tracking ref is old: it is moved there without a fetch, as a fetch would do
    [Fact]
    public void Run_WhenOnlyTheTrackingRefIsOld_UpdatesItWithoutFetching()
    {
        // Arrange
        string folder = Clone("A");
        string newCommit = PushNewCommit("b.txt");
        GitTestRepositories.RunGit("-C", folder, "fetch", _upstream, "main");
        GitTestRepositories.RunGit("-C", folder, "merge", "--ff-only", "FETCH_HEAD");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(new GitHead("main", newCommit), folder).Outcomes);

        // Assert
        Assert.True(outcome.WithoutRequest);
        Assert.Equal(newCommit, GitTestRepositories.RunGit("-C", folder, "rev-parse", "origin/main"));
        Assert.Equal(["update tracking ref"], _steps);
    }

    [Fact]
    public void Run_WhenTheFolderHasNoUpstream_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        string commit = Head(folder);
        GitTestRepositories.RunGit("-C", folder, "checkout", "--detach");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(new GitHead("main", commit), folder).Outcomes);

        // Assert
        Assert.StartsWith("has no upstream branch: git rev-parse @{u} failed", outcome.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenNoCommitMessageWasGiven_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        var sut = new GitRepositorySyncer(_commands, [GitFolderInspection.Inspect(_commands, Folder(folder))], null,
            new Dictionary<string, string>(), (_, step) => _steps.Add(step));

        // Act
        sut.Run();

        // Assert
        Assert.Equal("has changes, but no commit message was given", Assert.Single(sut.Outcomes).Error);
        Assert.Empty(_steps);
    }

    [Fact]
    public void Run_WhenTheCloneFails_ReportsTheErrorOfGit()
    {
        // Arrange
        var folder = new GitSyncFolder(GitName, Path.Combine(_tempFolder, "Missing.git"), "Project",
            Path.Combine(_tempFolder, "Projects", "New", GitName));
        var sut = new GitRepositorySyncer(_commands,
            [new GitFolderInspection(folder, EGitFolderInspectionKind.NeedsClone, null, null)], null,
            new Dictionary<string, string>(), (_, step) => _steps.Add(step));

        // Act
        sut.Run();

        // Assert
        GitFolderSyncOutcome outcome = Assert.Single(sut.Outcomes);
        Assert.False(outcome.Cloned);
        Assert.StartsWith("git clone -- ", outcome.Error, StringComparison.Ordinal);
    }

    //another git process holds the index (index.lock), so the commit cannot be made
    [Fact]
    public void Run_WhenTheCommitFails_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        GitFolderInspection inspection = GitFolderInspection.Inspect(_commands, Folder(folder));
        File.WriteAllText(Path.Combine(folder, ".git", "index.lock"), string.Empty);
        var sut = new GitRepositorySyncer(_commands, [inspection], null,
            new Dictionary<string, string> { [folder] = "Sync message" }, (_, step) => _steps.Add(step));

        // Act
        sut.Run();

        // Assert
        GitFolderSyncOutcome outcome = Assert.Single(sut.Outcomes);
        Assert.False(outcome.Committed);
        Assert.StartsWith("git commit -m Sync message failed", outcome.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_WhenTheFetchFails_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        GitTestRepositories.DeleteFolder(_remote);

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.False(outcome.Fetched);
        Assert.StartsWith("git remote update failed", outcome.Error, StringComparison.Ordinal);
    }

    //both sides changed the same line: the pull stops on the conflict, which is left for the user
    [Fact]
    public void Run_WhenThePullHitsAConflict_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "Readme.txt"), "changed here");
        PushNewCommit("Readme.txt");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.True(outcome.Committed);
        Assert.False(outcome.Pushed);
        Assert.StartsWith("git pull failed", outcome.Error, StringComparison.Ordinal);
    }

    //the remote refuses every push (its pre-receive hook): after the second refusal the folder fails
    [Fact]
    public void Run_WhenThePushIsRejectedAgain_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        File.WriteAllText(Path.Combine(_remote, "hooks", "pre-receive"), "#!/bin/sh\nexit 1\n");

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(null, folder).Outcomes);

        // Assert
        Assert.False(outcome.Pushed);
        Assert.StartsWith("git push failed", outcome.Error, StringComparison.Ordinal);
        Assert.Equal(["commit", "status", "fetch", "push", "fetch", "push"], _steps);
    }

    //another git process holds the tracking ref (its .lock file)
    [Fact]
    public void Run_WhenTheTrackingRefCannotBeUpdated_ReportsIt()
    {
        // Arrange
        string folder = Clone("A");
        string newCommit = PushNewCommit("b.txt");
        GitTestRepositories.RunGit("-C", folder, "fetch", _upstream, "main");
        GitTestRepositories.RunGit("-C", folder, "merge", "--ff-only", "FETCH_HEAD");
        string refsFolder = Path.Combine(folder, ".git", "refs", "remotes", "origin");
        Directory.CreateDirectory(refsFolder);
        File.WriteAllText(Path.Combine(refsFolder, "main.lock"), string.Empty);

        // Act
        GitFolderSyncOutcome outcome = Assert.Single(Run(new GitHead("main", newCommit), folder).Outcomes);

        // Assert
        Assert.False(outcome.Fetched);
        Assert.StartsWith("git update-ref refs/remotes/origin/main", outcome.Error, StringComparison.Ordinal);
    }

    //a folder of another branch is synced with a fetch, but the head of that branch does not replace the head of the
    //server's branch, so the next folder on the server's branch is still skipped
    [Fact]
    public void Run_WhenAFolderTracksAnotherBranch_KeepsTheKnownHeadOfTheServerBranch()
    {
        // Arrange
        string headOfMain = Head(_upstream);
        GitTestRepositories.RunGit("-C", _upstream, "checkout", "-q", "-b", "feature");
        File.WriteAllText(Path.Combine(_upstream, "f.txt"), "f");
        GitTestRepositories.RunGit("-C", _upstream, "add", "--all");
        GitTestRepositories.RunGit("-C", _upstream, "commit", "-q", "-m", "Feature");
        GitTestRepositories.RunGit("-C", _upstream, "push", "-q", "origin", "feature");
        string featureFolder = Clone("Feature");
        GitTestRepositories.RunGit("-C", featureFolder, "checkout", "-q", "-b", "feature", "--track",
            "origin/feature");
        string mainFolder = Clone("Main");

        // Act
        GitRepositorySyncer sut = Run(new GitHead("main", headOfMain), featureFolder, mainFolder);

        // Assert
        Assert.Equal([(true, null), (false, null)], sut.Outcomes.Select(x => (x.Fetched, x.Error)));
        Assert.True(sut.Outcomes[1].WithoutRequest);
    }

    //the second pass only catches up: a change made in the meantime stays for the next sync, uncommitted
    [Fact]
    public void Run_WhenAFolderChangesBeforeTheSecondPass_LeavesTheChange()
    {
        // Arrange
        string folderA = Clone("A");
        string folderB = Clone("B");
        File.WriteAllText(Path.Combine(folderA, "a.txt"), "a");
        File.WriteAllText(Path.Combine(folderB, "b.txt"), "b");
        List<GitFolderInspection> inspections =
        [
            GitFolderInspection.Inspect(_commands, Folder(folderA)),
            GitFolderInspection.Inspect(_commands, Folder(folderB))
        ];
        string lateFile = Path.Combine(folderA, "late.txt");
        var sut = new GitRepositorySyncer(_commands, inspections, null,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [folderA] = "Message A", [folderB] = "Message B"
            }, (folder, step) =>
            {
                if (folder.FolderPath == folderB && step == "push")
                {
                    File.WriteAllText(lateFile, "late");
                }
            });

        // Act
        sut.Run();

        // Assert
        Assert.All(sut.Outcomes, x => Assert.Null(x.Error));
        Assert.Equal("?? late.txt", GitTestRepositories.RunGit("-C", folderA, "status", "--porcelain"));
        Assert.NotEqual(GitTestRepositories.RunGit("-C", _remote, "rev-parse", "main"), Head(folderA));
    }

    //the clone was made before the push of the next folder, so the second pass brings it up to date
    [Fact]
    public void Run_WhenAFolderPushesAfterAClone_BringsTheCloneUpToDate()
    {
        // Arrange
        string clonedFolder = Path.Combine(_tempFolder, "Projects", "New", GitName);
        string aheadFolder = Clone("Ahead");
        File.WriteAllText(Path.Combine(aheadFolder, "a.txt"), "a");
        GitTestRepositories.RunGit("-C", aheadFolder, "add", "--all");
        GitTestRepositories.RunGit("-C", aheadFolder, "commit", "-q", "-m", "Not pushed yet");

        // Act
        GitRepositorySyncer sut = Run(null, clonedFolder, aheadFolder);

        // Assert
        Assert.Equal([(true, false, true), (false, true, false)],
            sut.Outcomes.Select(x => (x.Cloned, x.Pushed, x.Pulled)));
        Assert.Equal(Head(aheadFolder), Head(clonedFolder));
    }

    //a folder whose check failed is not touched again
    [Fact]
    public void Run_WhenTheCheckOfTheFolderFailed_LeavesIt()
    {
        // Arrange
        var inspection = new GitFolderInspection(Folder(Path.Combine(_tempFolder, "A")),
            EGitFolderInspectionKind.Failed, null, "is not a git repository with an origin");
        var sut = new GitRepositorySyncer(_commands, [inspection], null, new Dictionary<string, string>(),
            (_, step) => _steps.Add(step));

        // Act
        sut.Run();

        // Assert
        Assert.Equal("is not a git repository with an origin", Assert.Single(sut.Outcomes).Error);
        Assert.Empty(_steps);
    }

    private GitRepositorySyncer Run(GitHead? knownHead, params string[] folders)
    {
        List<GitFolderInspection> inspections = [.. folders.Select(x => GitFolderInspection.Inspect(_commands, Folder(x)))];
        Dictionary<string, string> commitMessages = inspections.Where(x => x.HasChanges)
            .ToDictionary(x => x.Folder.FolderPath, _ => "Sync message", StringComparer.OrdinalIgnoreCase);
        var sut = new GitRepositorySyncer(_commands, inspections, knownHead, commitMessages,
            (_, step) => _steps.Add(step));
        sut.Run();
        return sut;
    }

    private GitSyncFolder Folder(string path)
    {
        return new GitSyncFolder(GitName, _remote, "Project", path);
    }

    private string Clone(string name)
    {
        string folder = Path.Combine(_tempFolder, name);
        GitTestRepositories.RunGit("clone", _remote, folder);
        GitTestRepositories.ConfigureRepository(folder);
        return folder;
    }

    private string PushNewCommit(string fileName)
    {
        File.WriteAllText(Path.Combine(_upstream, fileName), fileName);
        GitTestRepositories.RunGit("-C", _upstream, "add", "--all");
        GitTestRepositories.RunGit("-C", _upstream, "commit", "-m", fileName);
        GitTestRepositories.RunGit("-C", _upstream, "push", "origin", "HEAD:main");
        return Head(_upstream);
    }

    private static string Head(string folder)
    {
        return GitTestRepositories.RunGit("-C", folder, "rev-parse", "HEAD");
    }

    //the hooks run in the sh of git, which takes C:/... paths
    private static string ShellPath(string path)
    {
        return path.Replace('\\', '/');
    }
}
