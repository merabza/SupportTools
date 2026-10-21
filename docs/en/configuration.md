# Configuration

All persistent state lives in a single JSON file managed by
`ParametersManager` (from the external `ParametersManagement`
library). This page describes the file's structure so you can find or
edit settings.

## File location

The parameters file path is supplied by the `--use` argument and
loaded by `ParametersService`. Without `--use`, `SupportTools.json` is
searched for in two places, in this order:

* the current directory
* the folder that holds the executable

The automatic search only uses a file that already exists — it never
offers to create one. With `--use`, a missing or invalid file is
offered for creation instead. The file name uses the date mask from `SupportToolsParameters.ParametersFileDateMask` and
the extension from `ParametersFileExtension`.

## Saving and backups

Every change made through the menus is saved at once. `ParametersManager`
writes the new content to a temporary file in the same folder and then
replaces the parameters file with it, so an interrupted save cannot leave
a damaged file. A save whose content did not change leaves the file
untouched.

Before the file is replaced, its previous version is copied next to it as
`<file name>.yyyyMMdd-HHmmss-fff.bak`, for example
`SupportTools.json.20261001-213015-123.bak`. The 10 newest copies are
kept and older ones are deleted. Only files named exactly this way are
deleted: copies made by hand under other names stay. The same applies to
every file saved through `ParametersManager`, for example the recent
commands file.

\---

## Top-level: `SupportToolsParameters`

Defined in `SupportToolsData/Models/SupportToolsParameters.cs`. Groups
of properties:

|Group|Properties|Used for|
|-|-|-|
|Paths|`LogFolder`, `WorkFolder`, `TempFolder`, `SecurityFolder`, `PublisherWorkFolder`, `CodeGenerateTestFolder`, `ScaffoldSeedersWorkFolder`, `FolderForGitignoreFiles`, `FolderForEditorConfigFiles`, `GitExecutablePath`|Where the tool reads/writes on disk; `FolderForGitignoreFiles` is the folder holding the `.gitignore` template files; `FolderForEditorConfigFiles` is the folder holding the `.editorconfig` template files; `GitExecutablePath` is the full path to the git executable (when empty, plain `git` from `PATH` is used; the editor auto-detects it via `Get-Command git` on Windows / `which git` on Linux)|
|Machine profile|`MachineName`, `CurrentMachineServerName`, `PathMappings`, `HideRemoteOnlyProjects`|Settings of this computer, never shared with other computers (see [Machine profile and path mapping](#machine-profile-and-path-mapping))|
|Registry sync|`SupportToolsServerWebApiClientName`, `AutoSyncWithSupportToolsServer`, `SupportToolsServerSyncTimeoutSeconds`, `RegistrySyncState`|The connection to SupportToolsServer and the sync state of this computer, never shared (see [Central registry](#central-registry-supporttoolsserver))|
|Exchange|`FileStorageNameForExchange`, `SmartSchemaNameForExchange`, `UploadTempExtension`|Required for AppSettings encode/install (see [Deployment](use-cases/deployment.md))|
|Recent commands|`RecentCommandsFileName`, `RecentCommandsCount`|Menu history|
|Archives|`ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`|Packaging conventions|
|Collections|`Projects`, `Servers`, `Gits`, `GitProjects`|All registered projects, server entries, git repos, and per-project git mappings|
|Templates|`Templates`, `ReactAppTemplates`, `NpmPackages`, `Environments`, `RunTimes`, `GitIgnorePatterns`, `EditorConfigPatterns`|Inputs to the project creator; `GitIgnorePatterns` / `EditorConfigPatterns` are also the templates that `.gitignore` / `.editorconfig` files are checked against (see [templates](#gitignore-and-editorconfig-templates))|
|Infrastructure|`DotnetTools`, `ApiClients`, `Archivers`, `DatabaseServerConnections`, `FileStorages`, `SmartSchemas`|Reusable shared resources|

\---

## Machine profile and path mapping

SupportTools runs on more than one computer. These fields belong to the
computer it runs on and are never shared with other computers. They are
edited at the top of `Support Tools Parameters Editor`:

* `MachineName` — the name of this computer. When it is empty, the name
from `Environment.MachineName` is used. Accepting the offered name leaves
the field empty, so a copied file still gives the right name on the other
computer; only a different name is stored.
* `CurrentMachineServerName` — the `Servers` entry that is this computer,
picked from the existing servers (`(None)` when there is no such entry).
When it is set, `IsLocal` is computed at start-up: only this server is
local, and the `Servers` editor no longer shows `IsLocal`. When it is
empty, the stored `IsLocal` values are used as before.
* `PathMappings` — rules that turn canonical paths into the paths of this
computer.
* `HideRemoteOnlyProjects` (default `false`) — after a registry sync every
computer lists every project, but a project is on this computer only when
its `ProjectFolderName` exists here. The project lists show `local` or
`remote` next to each project and `local: N, remote: N` next to each group;
with this setting on, the lists show only the local projects (and only the
groups that have one). See
[Git Operations](use-cases/git-operations.md#if-a-project-is-not-on-this-computer-yet).

Paths are kept in the canonical form: the Windows absolute path as on the
main computer (PAZISI), for example
`D:\1WorkDotnet\AppGrammarGe\AppGrammarGe\AppGrammarGe.slnx`. A rule maps a
canonical prefix to a local prefix, for example `D:\1WorkDotnet` →
`/home/merab/1WorkDotnet`:

* of several matching rules, the one with the longest prefix wins;
* a prefix matches only at a folder boundary: `D:\1WorkDotnet` does not
match `D:\1WorkDotnetX`;
* case and trailing separators are ignored;
* on Linux the separators in the rest of the path change too (`\` ↔ `/`);
* relative paths, such as `GitProjectFolderName`, only get their
separators changed;
* a path that no rule matches stays as it is; on Linux it is also recorded
as a warning;
* a Windows computer with the same folder layout needs no rules.

`Path Mappings` → `Suggest Path Mappings...` collects the roots (drive and
first folder, for example `D:\1WorkDotnet`) of the project paths, the
ServerInfo appsettings paths, the project creator folders and the local
file storage paths, and asks for the local prefix of each root. Enter keeps
the current value; the root itself or empty text means that the root needs
no rule.

The fields that stay on the computer and are never synced are listed in
`LibSupportToolsServerWork/Registry/MachineLocalFields.cs`.

\---

## Central registry (SupportToolsServer)

The shared collections of the parameters file are also kept on
SupportToolsServer, so every computer sees the same projects, servers,
templates and secret files. The local file stays the working copy and
works offline; for the shared data the server is the source of truth.

**What is shared and what stays here.** Every collection is shared
except the machine fields:

* the folders and tools of this computer (`LogFolder`, `WorkFolder`,
`TempFolder`, `SecurityFolder`, `FolderForGitignoreFiles`,
`FolderForEditorConfigFiles`, `GitExecutablePath`, …) and the recent
commands;
* the machine profile above and the fields of this section;
* `GitProjects`, which every computer computes for itself (see
[Git Operations](use-cases/git-operations.md#if-you-need-the-git-projects-without-cloning)),
and `Archivers`;
* inside shared records: `ServerDataModel.IsLocal`, the installed and
latest versions and the command name of a dotnet tool, and the local
folder of the database backup exchange.

The full list is `LibSupportToolsServerWork/Registry/MachineLocalFields.cs`.
Paths travel in the canonical form and are mapped by `PathMappings`.

**Fields:**

* `SupportToolsServerWebApiClientName` — the `ApiClients` entry that
points to SupportToolsServer (its address and API key). This bootstrap
API client is never synced: every computer has its own.
* `AutoSyncWithSupportToolsServer` (default `false`) — turns on auto-sync
on this computer (see below).
* `SupportToolsServerSyncTimeoutSeconds` (default `10`) — the time limit
of one auto-sync request.
* `RegistrySyncState` — written by the sync, not edited by hand. Per
collection it holds the server `Version` and a hash of every record at
the last successful sync, and the keys excluded from the sync on this
computer (`ExcludedKeys`); plus the time of the last sync
(`LastSyncUtc`) and the address of the server the state belongs to
(`ServerAddress`).

**Manual sync.** `Main Menu` → `Sync Registry With SupportToolsServer...`
compares every record on three sides: here, on the server and at the last
sync. A record changed on one side only is taken (pull) or sent (push); a
record changed on both sides is a conflict that you resolve. The command
first lists the warnings (for example a path that no `PathMappings` rule
maps) and a summary per collection, then offers:

* `Apply`, `Pull only`, `Push only` — each with the number of records it
sends and changes here;
* `Resolve conflicts` — `Local`, `Server` or `Skip` per conflict, with
`Show differences` field by field (secrets are shown as `***`);
* `Local wins for first sync conflicts` — for the first sync of the main
computer, which uploads everything (seed);
* `Show details`, `Dry run` (what `Apply` would send; nothing is sent);
* `Exclude record from sync` — keeps a record out of the sync on this
computer only, for example a database connection that differs on Linux:
it is neither sent nor taken;
* `Include excluded record again` — lists the excluded records and puts
one back into the sync. It is offered whenever a collection of the sync
has an excluded record, also when nothing else is to sync (then next to
`Finish`);
* `Cancel`.

The sync stops at the first record the server refuses and names it. After
a pull the previous parameters file is kept as a `.bak` copy (see
[Saving and backups](#saving-and-backups)). Deleting a secret file, in
either direction, is always asked as a conflict.

The `Gits`, `Git Ignore Patterns` and `Editor Config Patterns` list menus
have the same command for their collection only:
`Sync Gits With SupportToolsServer...`,
`Sync GitIgnorePatterns With SupportToolsServer...` and
`Sync EditorConfigPatterns With SupportToolsServer...`.

**Auto-sync.** With `AutoSyncWithSupportToolsServer` on, SupportTools
pulls the non-conflicting changes at start-up (one summary line above the
main menu; a record deleted on the server is not deleted here then) and
pushes after every save that changed shared data. Conflicts and records
the server refuses wait for the manual sync, and the warning says so.
Auto-sync works only with the server whose address is in
`RegistrySyncState.ServerAddress`, which every manual sync writes: after
pointing the API client to another server, run one manual sync. When the
server does not answer within the time limit, auto-sync is suspended
until SupportTools restarts and the local copy is used.

**What the server stores in plain text.** The server keeps the shared
records as they are, secrets included: API keys, database and file
storage user names and passwords, `KeyGuidPart`, `MediatRLicenseKey` and
the secret files that the registry points to
(`AppSettingsJsonSourceFileName` of the server infos,
`SeedProjectParametersFilePath`,
`PrepareProdCopyDatabaseProjectParametersFilePath`,
`PairedDbObjectsResultFileName`; a file inside a git working tree is left
out, git carries it). Protect the server, its database and its backups
like the parameters file. SupportTools never prints these values: a diff
shows `***`, and a secret file shows only its length and the start of its
hash.

\---

## `ProjectModel`

The core of each registered project. Located at
`SupportToolsData/Models/ProjectModel.cs`. Field groups:

|Group|Fields|
|-|-|
|Identity|`ProjectGroupName`, `ProjectName`, `ProjectDescription`, `ProjectFolderName`, `SolutionFileName`, `EditorConfigPatternName` (see [templates](#gitignore-and-editorconfig-templates))|
|Project type|`ProjectType` (Standard/IsService/IsPackage), `UseAlternativeWebAgent`, `UseLocalPackages` (own packages: off = `Distribute all packages` and `Reverse Package Distribution` skip the project)|
|Sub-project names|`MainProjectName`, `ApiContractsProjectName`, `SpaProjectName`, `DbContextProjectName`, `DbContextName`, `ProjectShortPrefix`|
|Migration \& seeding|`MigrationStartupProjectFilePath`, `MigrationProjectFilePath`, `SeedProjectFilePath`, `SeedProjectParametersFilePath`, `MigrationSqlFilesFolder`|
|Scaffold|`ScaffoldSeederProjectName`, `NewDataSeedingClassLibProjectName`, `ExcludesRulesParametersFilePath`|
|Encryption|`AppSetEnKeysJsonFileName`, `KeyGuidPart`|
|Database|`DevDatabaseParameters`, `ProdCopyDatabaseParameters`|
|Collections|`Endpoints`, `RouteClasses`, `ServerInfos`, `GitProjectNames`, `ScaffoldSeederGitProjectNames`, `FrontNpmPackageNames`, `RedundantFileNames`|

\---

## `ServerInfoModel` and `ServerDataModel`

A project's `ServerInfos` list is per-environment. Each entry holds
deployment routing for a specific environment-name + server-name
combination.

**`ServerInfoModel`** — per-project, per-environment:

* `EnvironmentName`, `ServerName` (composite key)
* `ServerSidePort`, `ApiVersionId` — required together for health checks
* `WebAgentNameForCheck` — which API client to use for version probes
* `AppSettingsJsonSourceFileName`, `AppSettingsEncodedJsonFileName` —
appSettings input and output paths
* `ServiceUserName` — service account on the target server
* `AllowToolsList` — which `EProjectServerTools` actions are enabled
* `CurrentDatabaseParameters`, `NewDatabaseParameters` — database
swap tracking during migrations

**`ServerDataModel`** — global, reused across projects:

* `IsLocal` — controls transport (`true` = local install folder,
`false` = WebAgent over HTTP). When `CurrentMachineServerName` is set, it
is computed (see [Machine profile and path mapping](#machine-profile-and-path-mapping))
* `WebAgentName`, `WebAgentInstallerName` — API client identifiers in
the `ApiClients` dictionary
* `FilesUserName`, `FilesUsersGroupName` — OS-level account
* `Runtime` — target .NET runtime
* `ServerSideDownloadFolder`, `ServerSideDeployFolder` — remote paths

\---

## `GitDataModel` and `GitProjectDataModel`

**`GitDataModel`** — a clone-able repository:

* `GitProjectAddress` — clone URL (SSH or HTTPS)
* `GitProjectFolderName` — local folder name to clone into
* `GitIgnorePatternName` — reference to a configured `.gitignore` template
(one of `GitIgnorePatterns`; its file is
`{FolderForGitignoreFiles}\{GitIgnorePatternName}.gitignore`)

**`GitProjectDataModel`** — maps a git repo to the `.csproj` files
inside it:

* `GitName` — links to a `GitDataModel`
* `ProjectRelativePath`, `ProjectFileName` — `.csproj` location inside
the repo
* `DependsOnProjectNames` — build-dependency hints for ordering

\---

## `.gitignore` and `.editorconfig` templates

Both kinds of templates are plain files in a templates folder, listed by
name in the parameters, and both lists are edited in
`Support Tools Parameters Editor` (`Git Ignore Patterns` /
`Editor Config Patterns`). Each list menu has `Check ... Files` (the
status shows how many files differ from their template or are missing),
`Update ... Files` (overwrites those files with the template) and
`Sync GitIgnorePatterns With SupportToolsServer...` /
`Sync EditorConfigPatterns With SupportToolsServer...`.

The sync command is the registry sync (see
[Central registry](#central-registry-supporttoolsserver)) limited to that
list: a record is the template's name and the content of its file, and it
matches the server's record by name, ignoring case. A template taken from
the server is written into the templates folder (created when missing)
and its name is added to the list. A template deleted on the server is
removed from the list here, but its file stays. A listed name without its
file, or the whole list when the templates folder is not set, is left out
of the sync with a warning. The server refuses to delete a template that
one of its git repos (`.gitignore`) or projects (`.editorconfig`) uses.

||`.gitignore`|`.editorconfig`|
|-|-|-|
|Bound to|a git repo: `GitDataModel.GitIgnorePatternName` (required)|a project: `ProjectModel.EditorConfigPatternName` (optional — a project without it is not checked)|
|Checked file|`.gitignore` in the repo folder, in every project that uses the repo|`.editorconfig` beside the project's `SolutionFileName`; projects without a solution file are skipped|
|Template file|`{FolderForGitignoreFiles}\{name}.gitignore`|`{FolderForEditorConfigFiles}\{name}.editorconfig`|
|New template from an existing file|project → Git menu → the repo → `Save .gitignore as New Template`|project → `Save .editorconfig as New Template`|

Saving a new template copies the file into the templates folder (the
folder is created if it does not exist) and adds the name to the list;
the repo's / project's own pattern name is not changed.

\---

## Encryption and secrets

Two project fields drive AppSettings encryption:

|Field|Role|
|-|-|
|`KeyGuidPart`|Per-project GUID. One half of the symmetric key.|
|`AppSetEnKeysJsonFileName`|JSON file listing which `appsettings.json` paths to encrypt.|

The actual key is `SHA256(KeyGuidPart + ServerName.Capitalize())` —
both halves must match between the encoder (developer machine) and the
decoder (target service).

Other potentially-sensitive collections in `SupportToolsParameters`:

* `DatabaseServerConnections` — DB connection strings with credentials
* `ApiClients` — endpoint URLs and API keys (used by WebAgent and the
SupportToolsServer)
* `FileStorages` — remote storage credentials

All of these are stored plaintext in the parameters JSON. Protect the
parameters file with file-system permissions. The central registry keeps
them in plain text on SupportToolsServer as well (see
[Central registry](#central-registry-supporttoolsserver)).

\---

## Editing

Most fields are editable through the in-app menu:

* `Support Tools Parameters Editor` — top-level fields, including the
`ApiClients`, `Servers` and `DatabaseServerConnections` collections
* `Support Tools Server Editor` — the records stored on
SupportToolsServer: `GitIgnore File Types`, `EditorConfig File Types`
and `Gits from SupportToolsServer`
* Per-project menus — project-specific fields, server infos, git lists

`Support Tools Server Editor` reads and writes the server directly, for
looking at the server records or fixing one. Before every write it reads
the server list again: when the record changed on the server since the
list was loaded (another computer, or a registry sync), or a new name
already exists there, the write is refused with `ConcurrencyConflict`
(`RecordWithNameNotFound` when it was deleted there) and the list is
loaded again. A change made here reaches the local
parameters with the next registry sync.

Deleting or renaming a git in `Gits from SupportToolsServer` also changes
the local `Gits` record once the server accepts the change. A deleted git
stays locally while a project still uses it. A rename also renames the
references in the projects (`GitProjectNames`,
`ScaffoldSeederGitProjectNames`) and in `GitProjects`, unless the new
name is already taken locally.

Editing the JSON file directly works for one-off fixes, but the menu
editors run validation that the file does not.

\---

## Related

* [Architecture](architecture.md) — which library owns which model
* [Deployment](use-cases/deployment.md) — how encryption is used
* [Development](development.md) — adding fields to a model

