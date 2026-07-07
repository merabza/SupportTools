# Configuration

All persistent state lives in a single JSON file managed by
`ParametersManager` (from the external `ParametersManagement`
library). This page describes the file's structure so you can find or
edit settings.

## File location

The parameters file path is supplied by `ArgumentsParser` and defaults
to a per-user location in the OS profile directory:

* Windows: `%USERPROFILE%\\<...>.json`
* Unix-like: `\~/<...>.json`

You can pass an alternate path as a CLI argument. The file name uses
the date mask from `SupportToolsParameters.ParametersFileDateMask` and
the extension from `ParametersFileExtension`.

\---

## Top-level: `SupportToolsParameters`

Defined in `SupportToolsData/Models/SupportToolsParameters.cs`. Groups
of properties:

|Group|Properties|Used for|
|-|-|-|
|Paths|`LogFolder`, `WorkFolder`, `TempFolder`, `SecurityFolder`, `PublisherWorkFolder`, `CodeGenerateTestFolder`, `ScaffoldSeedersWorkFolder`|Where the tool reads/writes on disk|
|Exchange|`FileStorageNameForExchange`, `SmartSchemaNameForExchange`, `UploadTempExtension`|Required for AppSettings encode/install (see [Deployment](use-cases/deployment.md))|
|Recent commands|`RecentCommandsFileName`, `RecentCommandsCount`|Menu history|
|Archives|`ProgramArchiveDateMask`, `ProgramArchiveExtension`, `ParametersFileDateMask`, `ParametersFileExtension`|Packaging conventions|
|Collections|`Projects`, `Servers`, `Gits`, `GitProjects`|All registered projects, server entries, git repos, and per-project git mappings|
|Templates|`Templates`, `ReactAppTemplates`, `NpmPackages`, `Environments`, `RunTimes`, `GitIgnoreModelFilePaths`|Inputs to the project creator|
|Infrastructure|`DotnetTools`, `ApiClients`, `Archivers`, `DatabaseServerConnections`, `FileStorages`, `SmartSchemas`|Reusable shared resources|

\---

## `ProjectModel`

The core of each registered project. Located at
`SupportToolsData/Models/ProjectModel.cs`. Field groups:

|Group|Fields|
|-|-|
|Identity|`ProjectGroupName`, `ProjectName`, `ProjectDescription`, `ProjectFolderName`, `SolutionFileName`|
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
* `GitIgnorePathName` — reference to a configured `.gitignore` template

**`GitProjectDataModel`** — maps a git repo to the `.csproj` files
inside it:

* `GitName` — links to a `GitDataModel`
* `ProjectRelativePath`, `ProjectFileName` — `.csproj` location inside
the repo
* `DependsOnProjectNames` — build-dependency hints for ordering

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

