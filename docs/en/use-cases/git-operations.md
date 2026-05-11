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
|`Git` submenu (per-project)|Manage individual repositories: list, add, remove, sync|
|`Git ScaffoldSeeder projects` submenu|Same, but for the project's scaffold seeder repos|

\---

## How sync works

Every sync command runs the same five-step pipeline against each repo:

1. **Clone** — `git clone <url> <path>` if the folder is missing
2. **Stage and inspect** — `git add .`, then check `git status --porcelain`
3. **Commit** — if there are uncommitted changes, prompt for a message
(reused across the batch) and run `git commit`
4. **Pull** — `git pull` if the remote has new commits
5. **Push** — `git push` if local has commits the remote does not

The processor classifies each repo into one of four states using
merge-base: `UpToDate`, `NeedToPull`, `NeedToPush`, or `Diverged`.

\---

## If you need to sync every repository at once

Use when you've been away for a while and want every project up to date.

**Steps**

1. `Main Menu` → `Sync All Projects All Gits V2`
2. Enter a commit message when prompted (used for any repos with local
changes)

**Expected output**

* Each repo reports its starting state and final state
* Repos with no changes complete instantly
* Repos with diverged history are skipped with an error — they need
manual resolution

**Common pitfalls**

* **Diverged branches.** If a repo has both local and remote commits,
the tool stops with an error. There is no automatic merge or rebase.
Resolve manually in the affected repo before re-running.
* **No locking between repos.** If two sync runs overlap (e.g., two
shells), repos sharing the same URL can race. Don't run sync
concurrently.
* **Reused commit message.** A single message is applied to every repo
in the batch. If you want per-repo messages, sync one project at a
time instead.
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

