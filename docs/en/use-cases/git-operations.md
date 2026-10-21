# Git Operations

SupportTools manages many projects, each of which can span multiple Git
repositories. The commands here let you keep all those repos in sync
without manually `cd`-ing into each one.

All commands assume each project has its Git repository URLs configured
via the project's `Git` submenu (see [Configuration](../configuration.md)).

## Quick Reference

|Command|Scope|
|-|-|
|`Sync All Projects All Gits V2`|Every repository across every project|
|`Sync One Group All Projects Gits V2`|Every repository across every project in one group|
|`Sync One Project All Gits With Scaffold Seeders V2`|All repositories of one project, including scaffold seeders|
|`Sync All Projects All Gits V3`, `Sync One Group All Projects Gits V3`, `Sync One Project All Gits With Scaffold Seeders V3`|The same scopes as V2, faster (see [below](#if-you-want-the-faster-sync-v3))|
|`Clone Project To This Computer`|A `remote` project: create its folder and clone its repositories here|
|`Update Git Projects` / `Update Git Projects From SupportToolsServer`|`GitProjects` of every repository, from the local clones or from the server's|
|`Sync Gits With SupportToolsServer...`|The list of git repositories, shared with the other computers|
|`Git` submenu (per-project)|Manage individual repositories: list, add, remove, sync|
|`Git ScaffoldSeeder projects` submenu|Same, but for the project's scaffold seeder repos|

\---

## How sync works

Every sync command runs the same five-step pipeline against each repo:

1. **Clone** — `git clone <url> <path>` if the folder is missing
2. **Stage and inspect** — `git add .`, then check `git status --porcelain`
3. **Commit** — if there are uncommitted changes, prompt for a message
and run `git commit`; in a batch you are also asked whether to reuse
that message for the remaining repos
4. **Pull** — `git pull` if the remote has new commits
5. **Push** — `git push` if local has commits the remote does not

The processor classifies each repo into one of four states using
merge-base: `UpToDate`, `NeedToPull`, `NeedToPush`, or `Diverged`.
A `Diverged` repo gets the same `git pull` as `NeedToPull` (Git merges
or rebases according to its `pull.rebase` setting) and the result is
then pushed; the repo stops with an error only if that pull fails, for
example on a merge conflict.

\---

## If you need to sync every repository at once

Use when you've been away for a while and want every project up to date.

**Steps**

1. `Main Menu` → `Sync All Projects All Gits V2`
2. If some projects are `remote` (not on this computer, see
[below](#if-a-project-is-not-on-this-computer-yet)), answer
`Include N remote-only projects (their gits will be cloned to this computer)?`.
The default `n` syncs only the local projects; `y` clones the remote ones
as well, as every sync did before.
3. Enter a commit message when prompted (used for any repos with local
changes)

**Expected output**

* Each repo reports its starting state and final state
* Repos with no changes complete instantly
* Repos with diverged history print a `Diverged` warning, then get
pulled and pushed; only a repo whose pull fails (e.g. a merge conflict)
stops with an error and needs manual resolution

**Common pitfalls**

* **Diverged branches.** If a repo has both local and remote commits,
the tool runs `git pull`, which merges or rebases according to your Git
`pull.rebase` setting, and then pushes. If the pull fails (for example
on a merge conflict), that repo stops with an error — resolve it
manually before re-running.
* **No locking between repos.** If two sync runs overlap (e.g., two
shells), repos sharing the same URL can race. Don't run sync
concurrently.
* **Reused commit message.** After the first message the tool asks
`Use this message for next commits?`. Answer `y` to apply it to every
remaining repo in the batch, or `n` to be asked again at the next
commit, with the previous message as the default.
* **Silent auth failure.** The tool relies on whatever Git credentials
your OS provides (SSH key, credential manager). If auth fails, the
output is a generic "cannot clone/push" — verify `git push` works
manually outside the tool first.

\---

## If you need to sync one group's repositories

Use when one team or one product area has been active and you want only
that scope updated.

**Steps**

1. `Main Menu` → `Project Groups List` → (select group) → `Sync One Group All Projects Gits V2`
2. As in the full sync, the group's `remote` projects are cloned only when
you answer `y` to the question about them.

**Expected output**

* Same per-repo report as the full sync, but limited to projects in the
chosen group

**Common pitfalls**

* **Cross-group dependencies.** If a project in this group depends on
shared code in another group's repo, syncing only this group may leave
you with stale shared code. Sync All is safer when in doubt.

\---

## If you need to sync one project (with its scaffold seeders)

Use when working on a single project and you want a tighter loop than
group-level sync.

**Steps**

1. `Main Menu` → `Project Groups List` → (group) → `Projects List` →
(project) → `Sync One Project All Gits With Scaffold Seeders V2`

**Expected output**

* All repos belonging to that project sync, plus any scaffold seeder
repos linked to it

**Common pitfalls**

* **Missing scaffold seeder folder.** If the configured scaffold
seeder work folder doesn't exist, it's logged as a warning, not an
error. The main repos still sync. Check the log if you expected
scaffold work.

\---

## If you want the faster sync (V3)

`Sync All Projects All Gits V3`, `Sync One Group All Projects Gits V3` and
`Sync One Project All Gits With Scaffold Seeders V3` sit right below their V2
commands and do the same work (clone, remove the ignored files from the
index, commit, pull, push), faster:

* SupportTools' own clone of every repository in `WorkFolder\Gits` is synced
last, so V2 finds it up to date. Nothing is committed there: local changes
are reported as an error.
* The remote commit of every repository comes from SupportToolsServer in one
request. The server keeps it for 10 minutes and checks an expired one with
`git ls-remote`. A folder without changes whose HEAD is that commit (on the
server's branch) makes no request to the git host at all.
* The folders are checked locally in parallel, 8 at a time, with one
`git status` each; the repositories are then synced in parallel, 8 at a time
(the folders of one repository one after another).
* Every question (the commit message) is asked before the parallel part.
* After a push SupportTools tells SupportToolsServer, which checks that remote
again and updates its clone.

**Steps**

Same menus and the same remote-only question as V2.

**Expected output**

* While it runs, the bottom of the console shows one line per running job,
updated in place, like `dotnet build`; results stay above it:
`RepoA  D:\...\RepoA: committed, pushed`, `...: pulled`, `...: cloned`, and
errors in red.
* `Checked N folders: ... with changes, ... to clone, ... failed`, then
`Remote heads from SupportToolsServer: X of Y gits` (and
`Checked with fetch: ...` for the repositories the server has no commit of).
* `Synced N gits, M folders: K without a request to the git server, ... fetched, ... cloned, ... committed, ... pulled, ... pushed, ... failed`.
* With redirected output (a file, another program) the board is not drawn;
only the result lines are written.

**Common pitfalls**

* **A push made outside SupportTools** (Visual Studio, the GitHub web site)
is not seen by the server until its 10 minutes are over: until then a clean
folder at the old commit is skipped. Changes are never lost: a push from such
a folder is rejected, and V3 fetches, merges and pushes again (once).
* **No SupportToolsServer** (`SupportToolsServerWebApiClientName` is empty, or
the server does not answer): a warning, then every repository is fetched
once; its other folders use that fetch.
* **No password prompts.** Git runs with `GIT_TERMINAL_PROMPT=0`, so a
repository that needs a typed password fails with git's error instead of
waiting for input. Use an SSH agent or a credential manager.
* **Merge conflicts** stop that folder with git's error, as in V2.
* **A branch that exists only here** (made with `git checkout -b` and never
pushed): V3 publishes it to `origin` under the same name
(`git push --set-upstream`) and writes `...: published branch <name>`; from
then on it is synced like any other branch. V2 stops such a folder with
`Unknown state`. If `origin` already has a branch of that name with commits
this one lacks, the push is rejected with git's error: run
`git branch -u origin/<name>` in the folder and sync again.

\---

## If a project is not on this computer yet

After a registry sync every computer lists every project, but each project
is physically on some computers only. A project is `local` when its
`ProjectFolderName` exists on this computer, otherwise `remote`. The
projects list shows the mark next to each project (`MyProject (remote)`),
and each group shows `local: N, remote: N`. `HideRemoteOnlyProjects` in
`Support Tools Parameters Editor` hides the remote projects from the lists.

The group commands work on the local projects by default:

* `Sync All Projects All Gits V2`, `Sync One Group All Projects Gits V2`, their
V3 versions, `Update Outdated Packages` and `Distribute all packages` ask whether to
include the remote projects too (default `n`), because their sync would
clone them here;
* `Check all projects build`, `Check group projects build`,
`Clear All Groups All Solutions All Projects`,
`Reverse Package Distribution`, `Remove Unused Package Versions` and
`Open all projects By Visual Studio` always skip the remote projects and
print how many they skipped: without the files they could only report
that the folders do not exist.

**Steps**

1. `Main Menu` → `Project Groups List` → (group) → `Projects List` →
(a `remote` project) → `Clone Project To This Computer` (shown only for
remote projects)
2. If the project has scaffold seeder repositories, answer whether to clone
them too (default `n`)
3. Confirm `Clone N gits of project ... to <folder>?`

**What it does**

1. Creates `ProjectFolderName`.
2. For each of the project's repositories that has no record in
`GitProjects`, runs the scan of `Update Git Projects` for that repository
only, and saves. This comes before the clone: a repository whose folder
name starts with `{SpaProjectFolderRelativePath}` needs the `GitProjects`
record of the project's `SpaProjectName` to know its folder.
3. Clones every repository with the same mechanism as the V2 sync.
4. Checks the template files: the `.gitignore` template of each repository
and the project's `.editorconfig` template exist, and the cloned files
match them.
5. Checks the project's secret files (`AppSettingsJsonSourceFileName` of
its server infos, `SeedProjectParametersFilePath`,
`PrepareProdCopyDatabaseProjectParametersFilePath`,
`PairedDbObjectsResultFileName`). When some are missing it lists them and
offers to run `Sync Registry With SupportToolsServer...`, which brings
them, then checks again.
6. Prints a report: the folder, the updated `GitProjects`, how many
repositories were cloned and which were not, the template problems and the
missing files.

**Common pitfalls**

* **Partial clone.** The folder is created first, so after a failed clone
the project already counts as `local`. Fix the cause (see the report) and
run `Sync One Project All Gits With Scaffold Seeders V2` on the project.
* **`GitProjects has no X (SpaProjectName of project Y)`.** The scan found
no project named like `SpaProjectName`. Check the name, or run
`Update Git Projects` (or `Update Git Projects From SupportToolsServer`)
from the Gits list. The V2 sync prints the same error instead of stopping
with an exception, and the project's other repositories still sync.

\---

## If you need the git projects without cloning

`GitProjects` maps every git repository to the `.csproj` / `.esproj`
files in it, with their dependencies; the V2 sync and the package commands
use it. Every computer computes it for itself and it is never synced. Both
commands are in `Support Tools Parameters Editor` → `Gits`:

* `Update Git Projects` scans the local clones;
* `Update Git Projects From SupportToolsServer` takes the same records from
SupportToolsServer, which scans its own clones of every repository, so a
new computer needs no clone. It replaces the whole `GitProjects` and
prints how many records were added, changed and removed. An empty server
list (the server has not scanned yet) or a server error changes nothing.

When a project name is in two repositories, the repository that is later
in name order wins, as in the local scan, and the name is reported.

\---

## If you need the git repositories on other computers

The list of git repositories (`Gits`) is part of the central registry
(see [Configuration](../configuration.md#central-registry-supporttoolsserver)):
`Sync Registry With SupportToolsServer...` and auto-sync carry it with the
rest. `Support Tools Parameters Editor` → `Gits` →
`Sync Gits With SupportToolsServer...` runs the same sync for the git
repositories only.

A repository's `.gitignore` template must be on the server before the
repository: sync `Git Ignore Patterns` first, or use the full sync, which
sends the templates first. SupportToolsServer clones every repository it
receives (for `Update Git Projects From SupportToolsServer`).

\---

## If you need to add, list, or remove a repository on a project

Use when bootstrapping a new project or onboarding a new repo.

**Steps**

1. `Main Menu` → `Project Groups List` → (group) → `Projects List` →
(project) → `Git` submenu
2. From the submenu, pick `New Git` to add, or select an existing entry
to remove/edit

**What the submenu offers**

* Load and save Git clone files (export/import the repo list)
* Add a new Git entry (URL, local folder name, branch info)
* Per-repo actions on each entry already registered

**Common pitfalls**

* **URL mismatch.** If you change a repo's remote URL in this submenu
but the local folder already points to a different remote, sync
detects the mismatch and stops. Either remove the local folder
manually or update the remote URL inside the existing checkout.
* **Folder collision.** Two entries that resolve to the same local
folder will overwrite each other on clone. The tool does not detect
this — keep folder names distinct.

\---

## Not yet implemented

The codebase contains a marker for planned future work
(`GitSubMenuCliMenuCommand.cs`): branch creation, branch switching, and
pull request management are not available yet. For now, do those
operations directly with your normal Git client.

\---

## Related

* [Configuration](../configuration.md) — how Git URLs are stored
* [Architecture](../architecture.md) — `LibGitWork`, `LibGitData`
* [Project Creation](project-creation.md) — initial repo setup for new
projects

