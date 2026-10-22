using System;
using System.Collections.Generic;
using System.Linq;
using SystemTools.SharedKernel;

namespace LibGitWork.SyncV3;

//ერთი git-ის ყველა ფოლდერის სინქრონიზაცია, თანმიმდევრობით. ფოლდერი git სერვერს არ მიმართავს, როცა მას ცვლილებები
//არ აქვს, მისი upstream remote-ის ცნობილი branch-ია და HEAD remote-ის ცნობილ commit-ზეა. ცნობილი ვერსია თავიდან
//SupportToolsServer-ისაა (ან არ არის), შემდეგ კი ყოველი fetch-ის და push-ის შედეგით ახლდება. დანარჩენი ფოლდერები V2-ის
//წესით სინქრონდება: commit, fetch (remote update), ჩამორჩენისას merge --ff-only, დაშორებისას pull, წინსწრებისას push.
//უარყოფილი push-ის შემდეგ ერთხელ ხელახლა fetch და სინქრონიზაცია ხდება. upstream-ის გარეშე branch-ი (აქ შექმნილი და ჯერ
//გამოუქვეყნებელი) fetch-ის შემდეგ origin-ზე იმავე სახელით ქვეყნდება
internal sealed class GitRepositorySyncer
{
    private const int MaxSyncSteps = 10;

    private readonly GitFolderCommands _commands;
    private readonly IReadOnlyDictionary<string, string> _commitMessages;
    private readonly List<GitFolderSyncOutcome> _outcomes;
    private readonly Action<GitSyncFolder, string> _reportStep;
    private GitHead? _knownHead;

    //commitMessages: ფოლდერის გზა => commit-ის მესიჯი (ცვლილებიანი ფოლდერებისთვის). reportStep: მიმდინარე ნაბიჯი
    public GitRepositorySyncer(GitFolderCommands commands, IEnumerable<GitFolderInspection> inspections,
        GitHead? knownHead, IReadOnlyDictionary<string, string> commitMessages,
        Action<GitSyncFolder, string> reportStep)
    {
        _commands = commands;
        _knownHead = knownHead;
        _commitMessages = commitMessages;
        _reportStep = reportStep;
        //ცვლილებიანი ფოლდერები პირველები (OrderBy სტაბილურია): მათი push-ის შემდეგ დანარჩენები ახალ ვერსიას ერთი გავლით იღებენ
        _outcomes = [.. inspections.OrderBy(x => x.HasChanges ? 0 : 1).Select(x => new GitFolderSyncOutcome(x))];
    }

    public IReadOnlyList<GitFolderSyncOutcome> Outcomes => _outcomes;

    public bool Pushed => _outcomes.Exists(x => x.Pushed);

    public void Run()
    {
        int lastPushIndex = -1;
        for (int i = 0; i < _outcomes.Count; i++)
        {
            SyncFolder(_outcomes[i], true);
            if (_outcomes[i].Pushed)
            {
                lastPushIndex = i;
            }
        }

        //ბოლო push-მდე დამუშავებულ ფოლდერებს ის ცვლილება არ აქვთ, რომელიც push-მა remote-ს დაუმატა: ისინი მეორე გავლით
        //ეწევიან remote-ს
        for (int i = 0; i < lastPushIndex; i++)
        {
            SyncFolder(_outcomes[i], false);
        }
    }

    private void SyncFolder(GitFolderSyncOutcome outcome, bool firstPass)
    {
        if (outcome.Error is not null)
        {
            return;
        }

        GitSyncFolder folder = outcome.Folder;
        if (firstPass && outcome.Inspection.Kind == EGitFolderInspectionKind.NeedsClone)
        {
            _reportStep(folder, "clone");
            Result cloneResult = _commands.Clone(folder.GitAddress, folder.FolderPath);
            if (cloneResult.IsFailure)
            {
                outcome.Error = cloneResult.Error.Description;
                return;
            }

            outcome.Cloned = true;
            return;
        }

        GitFolderStatus? status = firstPass ? outcome.Inspection.Status : ReadStatus(outcome);
        //მეორე გავლაში გაჩენილი ცვლილებები არ კომიტდება, ისინი შემდეგ სინქრონიზაციას რჩება
        if (status is null || !firstPass && status.HasChanges)
        {
            return;
        }

        if (status.HasChanges)
        {
            if (!_commitMessages.TryGetValue(folder.FolderPath, out string? commitMessage))
            {
                outcome.Error = "has changes, but no commit message was given";
                return;
            }

            _reportStep(folder, "commit");
            Result commitResult = _commands.Commit(folder.FolderPath, commitMessage);
            if (commitResult.IsFailure)
            {
                outcome.Error = commitResult.Error.Description;
                return;
            }

            outcome.Committed = true;
            status = ReadStatus(outcome);
            if (status is null)
            {
                return;
            }
        }

        if (CanSkip(status))
        {
            UpdateTrackingRefIfDiffers(outcome, status);
            return;
        }

        _reportStep(folder, "fetch");
        Result fetchResult = _commands.RemoteUpdate(folder.FolderPath);
        if (fetchResult.IsFailure)
        {
            outcome.Error = fetchResult.Error.Description;
            return;
        }

        outcome.Fetched = true;
        if (status.BranchWithoutUpstream is not null)
        {
            PublishBranch(outcome, status.BranchWithoutUpstream);
            return;
        }

        SyncWithUpstream(outcome, status.UpstreamBranch);
    }

    //branch origin-ზე იმავე სახელით იგზავნება და მისი upstream ხდება, ამიტომ შემდეგი სინქრონიზაცია მას ჩვეულებრივად
    //ადარებს. თუ origin-ს ამ სახელის branch უკვე აქვს და მასზე აქ უცნობი commit-ებია, push უარყოფილია და შეცდომად რჩება
    private void PublishBranch(GitFolderSyncOutcome outcome, string branchName)
    {
        _reportStep(outcome.Folder, "publish");
        Result pushResult = _commands.PushSetUpstream(outcome.Folder.FolderPath, branchName);
        if (pushResult.IsFailure)
        {
            outcome.Error = pushResult.Error.Description;
            return;
        }

        outcome.Pushed = true;
        outcome.PublishedBranch = branchName;
    }

    private bool CanSkip(GitFolderStatus status)
    {
        return _knownHead is not null && !status.HasChanges && status.UpstreamBranch == _knownHead.BranchName &&
               status.HeadCommit == _knownHead.CommitId;
    }

    //HEAD remote-ის ვერსიაზეა, მაგრამ ლოკალური refs/remotes/origin/<branch> ძველია: ის ისე ახლდება, როგორც ამას fetch
    //გააკეთებდა, რომ git status-მა ტყუილი "ahead" არ აჩვენოს
    private void UpdateTrackingRefIfDiffers(GitFolderSyncOutcome outcome, GitFolderStatus status)
    {
        if (!status.TrackingRefDiffers || _knownHead is null)
        {
            return;
        }

        _reportStep(outcome.Folder, "update tracking ref");
        Result updateResult =
            _commands.UpdateTrackingRef(outcome.Folder.FolderPath, _knownHead.BranchName, _knownHead.CommitId);
        if (updateResult.IsFailure)
        {
            outcome.Error = updateResult.Error.Description;
        }
    }

    private void SyncWithUpstream(GitFolderSyncOutcome outcome, string? upstreamBranch)
    {
        GitSyncFolder folder = outcome.Folder;
        bool pushRetried = false;
        for (int step = 0; step < MaxSyncSteps; step++)
        {
            (GitState state, string? upstreamCommit, string? error) = GetState(folder.FolderPath);
            Result result;
            switch (state)
            {
                case GitState.UpToDate:
                    RememberHead(upstreamBranch, upstreamCommit!);
                    return;
                case GitState.NeedToPull:
                    _reportStep(folder, "merge");
                    result = _commands.MergeFastForward(folder.FolderPath);
                    outcome.Pulled |= result.IsSuccess;
                    break;
                case GitState.Diverged:
                    _reportStep(folder, "pull");
                    result = _commands.Pull(folder.FolderPath);
                    outcome.Pulled |= result.IsSuccess;
                    break;
                case GitState.NeedToPush:
                    _reportStep(folder, "push");
                    result = _commands.Push(folder.FolderPath);
                    outcome.Pushed |= result.IsSuccess;
                    if (result.IsFailure && !pushRetried)
                    {
                        //remote push-ის წინა fetch-ის შემდეგ შეიცვალა: ახალი fetch და სინქრონიზაცია თავიდან
                        pushRetried = true;
                        _reportStep(folder, "fetch");
                        result = _commands.RemoteUpdate(folder.FolderPath);
                    }

                    break;
                default:
                    outcome.Error = error;
                    return;
            }

            if (result.IsFailure)
            {
                outcome.Error = result.Error.Description;
                return;
            }
        }

        outcome.Error = $"the sync did not finish in {MaxSyncSteps} steps";
    }

    //V2-ის GitProcessor.GetGitState-ის წესი: HEAD, upstream და მათი საერთო წინაპარი
    private (GitState State, string? UpstreamCommit, string? Error) GetState(string folderPath)
    {
        Result<string> localResult = _commands.RevParse(folderPath, "@");
        if (localResult.IsFailure)
        {
            return (GitState.Unknown, null, localResult.Error.Description);
        }

        Result<string> upstreamResult = _commands.RevParse(folderPath, "@{u}");
        if (upstreamResult.IsFailure)
        {
            return (GitState.Unknown, null, $"has no upstream branch: {upstreamResult.Error.Description}");
        }

        string local = localResult.Value;
        string upstream = upstreamResult.Value;
        if (local == upstream)
        {
            return (GitState.UpToDate, upstream, null);
        }

        Result<string> baseResult = _commands.MergeBase(folderPath);
        if (baseResult.IsFailure)
        {
            return (GitState.Unknown, null, baseResult.Error.Description);
        }

        if (local == baseResult.Value)
        {
            return (GitState.NeedToPull, upstream, null);
        }

        return upstream == baseResult.Value
            ? (GitState.NeedToPush, upstream, null)
            : (GitState.Diverged, upstream, null);
    }

    //სინქრონიზებული ფოლდერის upstream ახლა remote-ის ვერსიაა: იმავე git-ის მომდევნო ფოლდერები მას ადარებენ.
    //სხვა branch-ის ფოლდერი SupportToolsServer-ის ცნობილ ვერსიას არ ცვლის
    private void RememberHead(string? upstreamBranch, string upstreamCommit)
    {
        if (upstreamBranch is not null && (_knownHead is null || _knownHead.BranchName == upstreamBranch))
        {
            _knownHead = new GitHead(upstreamBranch, upstreamCommit);
        }
    }

    private GitFolderStatus? ReadStatus(GitFolderSyncOutcome outcome)
    {
        _reportStep(outcome.Folder, "status");
        Result<GitFolderStatus> statusResult = _commands.ReadStatus(outcome.Folder.FolderPath);
        if (statusResult.IsSuccess)
        {
            return statusResult.Value;
        }

        outcome.Error = statusResult.Error.Description;
        return null;
    }
}
