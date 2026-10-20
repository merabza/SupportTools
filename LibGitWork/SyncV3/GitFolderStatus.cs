using System;

namespace LibGitWork.SyncV3;

//git status --porcelain=v2 --branch-ის შედეგი: ერთი პროცესით ცნობილია HEAD-ის commit, upstream branch და ცვლილებები.
//HeadCommit null-ია ჯერ commit-ის გარეშე რეპოზიტორიაში, UpstreamBranch კი - როცა upstream არ არის ან ის origin-ის არ არის.
//TrackingRefDiffers ნიშნავს, რომ ლოკალური refs/remotes/origin/<branch> HEAD-ს არ ემთხვევა (ან არ არსებობს)
internal sealed record GitFolderStatus(
    string? HeadCommit,
    string? UpstreamBranch,
    bool TrackingRefDiffers,
    bool HasChanges)
{
    private const string OriginPrefix = "origin/";

    //HEAD-ის branch, როცა მას upstream საერთოდ არ აქვს (მაგალითად, აქ შექმნილი და ჯერ გამოუქვეყნებელი). null-ია, როცა
    //upstream აქვს (სხვა remote-ისაც) ან HEAD detached-ია
    public string? BranchWithoutUpstream { get; init; }

    public static GitFolderStatus Parse(string output)
    {
        string? headCommit = null;
        string? branchName = null;
        bool hasUpstream = false;
        string? upstreamBranch = null;
        bool trackingRefDiffers = true;
        bool hasChanges = false;
        foreach (string line in output.Split('\n',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            //სათაურის სტრიქონები "# "-ით იწყება, ფაილის ყოველი ცვლილება კი სხვა სიმბოლოთი
            if (!line.StartsWith('#'))
            {
                hasChanges = true;
                continue;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (parts)
            {
                case [_, "branch.oid", var oid]:
                    headCommit = oid == "(initial)" ? null : oid;
                    break;
                case [_, "branch.head", var head]:
                    branchName = head == "(detached)" ? null : head;
                    break;
                case [_, "branch.upstream", var upstream]:
                    hasUpstream = true;
                    upstreamBranch = upstream.StartsWith(OriginPrefix, StringComparison.Ordinal)
                        ? upstream[OriginPrefix.Length..]
                        : null;
                    break;
                case [_, "branch.ab", var ahead, var behind]:
                    trackingRefDiffers = ahead != "+0" || behind != "-0";
                    break;
            }
        }

        return new GitFolderStatus(headCommit, upstreamBranch, trackingRefDiffers, hasChanges)
        {
            BranchWithoutUpstream = hasUpstream ? null : branchName
        };
    }
}
