namespace LibGitWork.SyncV3;

//remote-ის branch და მისი commit: SupportToolsServer-ისგან მიღებული, ან ამავე სინქრონიზაციის fetch-ით და push-ით ნანახი
internal sealed record GitHead(string BranchName, string CommitId);
