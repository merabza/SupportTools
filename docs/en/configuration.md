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

\---

## Top-level: `SupportToolsParameters`

Defined in `SupportToolsData/Models/SupportToolsParameters.cs`. Groups
of properties:

|Group|Properties|Used for|
|-|-|-|
|Paths|`LogFolder`, `WorkFolder`, `TempFolder`, `SecurityFolder`, `PublisherWorkFolder`, `CodeGenerateTestFolder`, `ScaffoldSeedersWorkFolder`, `FolderForGitignoreFiles`, `FolderForEditorConfigFiles`, `GitExecutablePath`|Where the tool reads/writes on disk; `FolderForGitignoreFiles` is the folder holding the `.gitignore` template files; `FolderForEditorConfigFiles` is the folder holding the `.editorconfig` template files; `GitExecutablePath` is the full path to the git executable (when empty, plain `git` from `PATH` is used; the editor auto-detects it via `Get-Command git` on Windows / `which git` on Linux)|
|Exchange|`FileStorageNameForExchange`, `SmartSchemaNameForExchange`, `UploadTempExtension`|Required for AppSettings encode/install (see [Deployment](use-cases/deployment.md))|
|Recent commands|`RecentCommandsFileName`, `RecentCommandsCount`|Menu history|
|Archives|`ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`|Packaging conventions|
|Collections|`Projects`, `Servers`, `Gits`, `GitProjects`|All registered projects, server entries, git repos, and per-project git mappings|
|Templates|`Templates`, `ReactAppTemplates`, `NpmPackages`, `Environments`, `RunTimes`, `GitIgnorePatterns`, `EditorConfigPatterns`|Inputs to the project creator; `GitIgnorePatterns` / `EditorConfigPatterns` are also the templates that `.gitignore` / `.editorconfig` files are checked against (see [templates](#gitignore-and-editorconfig-templates))|
|Infrastructure|`DotnetTools`, `ApiClients`, `Archivers`, `DatabaseServerConnections`, `FileStorages`, `SmartSchemas`|Reusable shared resources|

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
`false` = WebAgent over HTTP)
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
status shows how many files differ from their template or are missing)
and `Update ... Files` (overwrites those files with the template).

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

* `Support Tools Parameters Edit` — top-level fields
* `Support Tools Server Edit` — `ApiClients`, `Servers`,
`DatabaseServerConnections`
* Per-project menus — project-specific fields, server infos, git lists

Editing the JSON file directly works for one-off fixes, but the menu
editors run validation that the file does not.

\---

## Related

* [Architecture](architecture.md) — which library owns which model
* [Deployment](use-cases/deployment.md) — how encryption is used
* [Development](development.md) — adding fields to a model

