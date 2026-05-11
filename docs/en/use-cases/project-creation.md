# Project Creation

Bootstrap new projects from templates, import existing project
configurations, and keep dependencies up to date.

## Quick Reference

|Command|Purpose|
|-|-|
|`Create New Project` (template)|Generate a new solution from a template|
|`Import Project`|Register a previously exported project into local state|
|`Update Outdated Packages`|Sync repos and update NuGet packages across projects|
|`Clear All Groups All Solutions All Projects`|Wipe local working directories for all projects|

\---

## Templates

The template factory (`AppCreatorFactory`) currently produces four kinds:

|Template|Output|
|-|-|
|`Console`|CLI application, optionally with database/repositories/migrations|
|`Api`|REST API, optionally with database, carcass framework, identity, SignalR|
|`Razor`|Server-rendered web app (minimal extra projects)|
|`ScaffoldSeeder`|Listed but currently returns no creator — not functional|

A `TemplateModel` controls the features each generated solution gets:
database, menu support, HTTPS, React frontend, carcass, Identity,
ReCounter, SignalR, FluentValidation, and which test projects to add.

\---

## If you need a new solution from a template

Use when starting a new project from scratch.

**Steps**

1. `Main Menu` → `Project Creator` submenu → select the template
2. Fill in the required fields:
* Project name (will be normalized to a valid class-name)
* Solution folder name
* Work folder path (where the solution will live)
* Security folder path (where secrets and key material are kept)
* Project type (Console / Api / Razor)
* For API projects with a database: short name and db part folder name
* Indent size for generated code
3. Optionally fill database parameters (dev + prod copy)

**Expected output**

* A new solution at the work folder path
* Project registered in SupportTools state under the chosen group
* Database settings recorded if provided

**Common pitfalls**

* **Missing security folder.** The creator logs an error and stops, but
doesn't re-prompt. Make sure the security folder exists and is
writable before starting.
* **Name collision.** An existing project with the same name aborts
creation. Rename and retry.
* **API without short name.** API templates need a short name and db
part folder for the database-related projects to be generated. Skip
these and you get an API without the data layer scaffolded.

\---

## If you need to register a previously exported project

Use when bringing a project's configuration onto a new machine.

This is **not Git clone**. Import reads a JSON file produced by
`Export Project` (from another machine) and registers its contents in
local state.

**Steps**

1. `Main Menu` → `Import Project`
2. Point to the exported JSON file

**What it does**

* Deserializes the JSON
* Validates that no project with this name already exists
* Clears `ServerInfos` and `AllowToolsList` on the imported project
(server assignments and per-tool toggles do not survive export/import)
* Adds the project and its declared Git repos to local parameters
* Persists the change

**Common pitfalls**

* **Server assignments lost.** Importing strips the project's server
list. You must redo `Create New Server Info` after import.
* **Allow-tools list lost.** The list of enabled per-project tools is
cleared. Reconfigure via `Select Project Allow Tools`.
* **Silent property drop.** Unknown fields in the JSON (e.g. from a
newer export format) are ignored without warning. Verify imported
settings look right before relying on them.
* **Git entry collisions.** If a Git entry from the import shares its
key with an existing one, the existing entry is overwritten — useful
for refreshes, dangerous for accidental overwrites.

\---

## If you need to update NuGet packages across projects

Use during routine maintenance to bump dependencies.

**Steps**

1. `Main Menu` → `Update Outdated Packages`

**What it does**

* Syncs all Git repositories first (so updates land on current code)
* Iterates over projects and identifies dependencies
* Updates NuGet package versions in each `.csproj`
* Re-syncs after each project's updates

This is `.csproj`-based; it does **not** invoke `dotnet outdated`, and
it does **not** consult `Directory.Packages.props` centrally.

**Common pitfalls**

* **No pre-check that `.csproj` files exist.** A misconfigured project
can cause errors mid-run; the run continues for other projects but
that one is incomplete.
* **No rollback on failure.** Partial updates persist if a project
fails. Use Git to revert if needed.
* **Re-sync between projects.** If you have local uncommitted changes
in a repo, the post-update sync will try to commit them with the same
batch message — verify the working tree is clean before starting.

\---

## If you need to wipe local working directories

Use when reclaiming disk space, or before a clean re-clone of everything.

**This is destructive.** It runs without confirmation and removes all
project working directories.

**Steps**

1. `Main Menu` → `Clear All Groups All Solutions All Projects`

**What it does**

* Iterates all projects (or a filtered subset)
* Calls `Clear One Solution` for each Git collection, including
scaffold seeder collections
* Deletes the project working folders
* Excludes the tool's own `bin/Debug` directory from deletion

**Expected output**

* All registered project folders on disk are gone
* SupportTools state (the JSON parameter file) is untouched — projects
remain registered, ready to be re-cloned by `Sync All Projects All Gits V2`

**Common pitfalls**

* **No prompt.** As soon as you select the command, deletion starts.
There's no "are you sure?" guard.
* **Folder configuration matters.** If a project's work-folder path
points to a parent directory by mistake, the clear can reach further
than intended. Double-check folder paths in project parameters before
running.
* **Bin/Debug exclusion is location-sensitive.** The exclusion only
works when the tool runs from its standard build output. Running from
an unusual path can confuse the exclusion logic.

\---

## Related

* [Git Operations](git-operations.md) — what `Sync` does inside `Update Outdated Packages` and after import
* [Configuration](../configuration.md) — where exported/imported state
lives
* [Architecture](../architecture.md) — `LibAppProjectCreator`

