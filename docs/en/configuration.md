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
|Machine profile|`MachineName`, `CurrentMachineServerName`, `PathMappings`|Settings of this computer, never shared with other computers (see [Machine profile and path mapping](#machine-profile-and-path-mapping))|
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

## `ProjectModel`

The core of each registered project. Located at
`SupportToolsData/Models/ProjectModel.cs`. Field groups:

|Group|Fields|
|-|-|
|Identity|`ProjectGroupName`, `ProjectName`, `ProjectDescription`, `ProjectFolderName`, `SolutionFileName`, `EditorConfigPatternName` (see [templates](#gitignore-and-editorconfig-templates))|
|Project type|`ProjectType` (Standard/IsService/IsPackage), `UseAlternativeWebAgent`|
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
`Sync ... files...`.

`Sync ... files...` compares the list with the templates that
SupportToolsServer stores in its database. Records match by name,
ignoring case. Every template file of the list must exist, otherwise
the command stops. It first shows the differences: templates whose
content differs, and templates that exist only on the client or only on
the server. Then it offers four directions, each with a summary of what
it would change:

* `Merge Up` — uploads the new and changed templates to the server;
* `Sync Up` — the same, and also deletes the server records that are
not in the list;
* `Merge Down` — writes the new and changed server templates into the
templates folder and adds the new names to the list;
* `Sync Down` — the same, and also removes the templates that are not
on the server from the list and deletes their files.

`Merge` never deletes anything. A template that is in use is not
deleted: a `.gitignore` template that a git repo on the same side uses
(`Gits` on the client, the server's git repos), or an `.editorconfig`
template that a project uses (on the client only; nothing uses them on
the server).

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
parameters file with file-system permissions.

\---

## Editing

Most fields are editable through the in-app menu:

* `Support Tools Parameters Editor` — top-level fields, including the
`ApiClients`, `Servers` and `DatabaseServerConnections` collections
* `Support Tools Server Editor` — the records stored on
SupportToolsServer: `GitIgnore File Types`, `EditorConfig File Types`
and `Gits from SupportToolsServer`
* Per-project menus — project-specific fields, server infos, git lists

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

