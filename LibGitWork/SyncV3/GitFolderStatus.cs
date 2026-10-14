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

    public static GitFolderStatus Parse(string output)
    {
        string? headCommit = null;
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
                case [_, "branch.upstream", var upstream]:
                    upstreamBranch = upstream.StartsWith(OriginPrefix, StringComparison.Ordinal)
                        ? upstream[OriginPrefix.Length..]
                        : null;
                    break;
                case [_, "branch.ab", var ahead, var behind]:
                    trackingRefDiffers = ahead != "+0" || behind != "-0";
                    break;
            }
        }

        return new GitFolderStatus(headCommit, upstreamBranch, trackingRefDiffers, hasChanges);
    }
}
