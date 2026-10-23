# Architecture

SupportTools is a modular .NET 10 console application. The solution is
split into one entry-point executable, one application-bootstrap project,
one data project, and eleven `Lib\*` libraries grouped by domain.

The solution also pulls in nine external repositories as sibling
checkouts (see [Getting Started](getting-started.md) for the clone list).
References to those are kept at the `.csproj` level via relative paths;
the build expects all sibling repos to live alongside `SupportTools/`.

\---

## Module reference

|Module|Purpose|Key dependencies|
|-|-|-|
|`SupportToolsData`|Domain models, enums (`EProjectTools`), JSON state shape|— (no internal deps)|
|`LibGitData`|Data structures for Git repository metadata|SystemTools.SystemToolsShared|
|`LibTools`|Shared helpers and caching|LibGitWork, SupportToolsData|
|`LibDotnetWork`|`dotnet` CLI orchestration (build, restore, ef)|SystemTools.SystemToolsShared|
|`LibGitWork`|`git` command execution and repo manipulation|SystemTools.SystemToolsShared|
|`LibNpmWork`|npm orchestration for frontend tasks|SystemTools.SystemToolsShared|
|`LibDatabaseWork`|Database lifecycle: create, drop, migrate, correct|LibDotnetWork, SupportToolsData|
|`LibCodeGenerator`|Code generation (API routes, project artifacts)|LibGitWork, SupportToolsData|
|`LibAppInstallWork`|Install/uninstall pipelines (local \& remote)|LibDotnetWork, SupportToolsData|
|`LibSupportToolsServerWork`|The central registry: sync of the shared collections with SupportToolsServer (see [Registry layer](#registry-layer)); the SupportToolsServer editors (`Support Tools Server Editor`); all through the server client `SupportToolsServerApiClient`|LibGitWork, SupportToolsData|
|`LibAppProjectCreator`|Project scaffolding from templates|LibAppInstallWork, LibGitWork, LibNpmWork|
|`SupportTools.Application`|DI host, configuration, parameter management|External: AppCliTools.CliTools, ParametersManagement.\*|
|`SupportTools`|Console entry point, menu wiring, CLI command factories|All Lib\* projects|

\---

## Dependency layers

```
Layer 5  Entry
         ┌──────────────────────────────────────────────────────────┐
         │ SupportTools (Exe)                                       │
         │ SupportTools.Application                                 │
         └──────────────────────────────────────────────────────────┘
                                  │
Layer 4  High-level workflows     ▼
         ┌──────────────────────────────────────────────────────────┐
         │ LibAppProjectCreator                                     │
         └──────────────────────────────────────────────────────────┘
                                  │
Layer 3  Domain logic             ▼
         ┌──────────────────────────────────────────────────────────┐
         │ LibDatabaseWork  LibCodeGenerator                        │
         │ LibAppInstallWork  LibSupportToolsServerWork             │
         └──────────────────────────────────────────────────────────┘
                                  │
Layer 2  Process orchestration    ▼
         ┌──────────────────────────────────────────────────────────┐
         │ LibDotnetWork  LibGitWork  LibNpmWork                    │
         └──────────────────────────────────────────────────────────┘
                                  │
Layer 1  Foundation               ▼
         ┌──────────────────────────────────────────────────────────┐
         │ SupportToolsData  LibGitData  LibTools                   │
         └──────────────────────────────────────────────────────────┘
```

Reads from top: an executable command in `SupportTools/Menu/` uses an
orchestrator in `LibAppInstallWork` or `LibAppProjectCreator`, which
delegates to `LibDotnetWork`/`LibGitWork`/`LibNpmWork` for process work,
which uses `LibGitData`/`SupportToolsData` for typed state.

\---

## External repositories the solution depends on

These are sibling Git checkouts referenced via relative paths in
`SupportTools.slnx`. They are maintained as separate repositories:

|Repo|Used for|
|-|-|
|`AppCliTools`|CLI menu, parameter editing, data input|
|`ConnectionTools`|FTP/connection-pool utilities|
|`DatabaseTools`|SQL Server, SQLite, OleDb, MariaDB adapters|
|`ParametersManagement`|API client, file, and database parameter management|
|`SupportToolsServerShared`|Contracts shared with SupportToolsServer|
|`SystemTools`|System abstractions, background tasks, database tools shared types|
|`ToolsManagement`|API clients, compression, databases, file managers, installer|
|`WebAgentContracts`|Contracts for the WebAgent API (databases, projects)|

\---

## Entry point

`SupportTools/Program.cs` does the standard CLI bootstrap:

1. `ArgumentsAnalyzer` reads CLI args;
`ParametersService<SupportToolsParameters>` loads the parameters file
2. `ServiceCollection.AddServices(...)` (in
`SupportTools.Application`) wires up DI
3. `CliAppLoopParameters.Create<Program>(...)` builds the menu loop
4. `CliAppLoop.Run()` runs until the user exits

Menu commands are registered as factory strategies in
`SupportTools/Menu/`. The main menu is assembled in
`SupportToolsMenuBuilder` from `MenuData.MainMenuCommandFactoryStrategyNames`.

\---

## Registry layer

The shared collections of the parameters file (projects, servers, git
repos, templates, API clients, …) are also kept on SupportToolsServer,
the central registry, so that every computer sees the same data (see
[Configuration](configuration.md#central-registry-supporttoolsserver)).
`LibSupportToolsServerWork/Registry/` holds the sync:

|Part|Role|
|-|-|
|`Paths/`|`PathMapper`: the canonical paths (Windows, as on the main computer) ↔ the paths of this computer, by `PathMappings`|
|`Mappers/`|One pure mapper per collection: the local model ↔ the server's contract (`Sts…DataModel`), with canonical paths and a normalized form for the hash|
|`Adapters/`|One adapter per collection (18 of them): reads the local and the server records and writes either side. `RegistrySyncAdapterFactory` builds them; `RegistryCollections` names them and sets their order (the referenced collections first, `Projects` last)|
|`Sync/`|The engine: `RegistrySyncPlanner` compares every record here, on the server and at the last sync (`RegistrySyncState`: the server `Version` and a hash per record) and plans a pull, a push or a conflict; `RegistrySyncEngine` runs the chosen part and saves the parameters once|
|`MachineLocalFields`|The fields that never sync|

The engine has no UI. It is used by the `SupportTools` project:

* `Menu/SyncRegistry/` — `Sync Registry With SupportToolsServer...` and
its one-collection shortcuts in the `Gits` and template list menus;
* `AutoSync/` — the pull at start-up and the push after every save: the
`IParametersManager` of DI is the decorator `AutoSyncParametersManager`;
* `Menu/ProjectGroupsList/ProjectsList/CloneProjectToThisComputer/` —
brings a `remote` project to this computer.

`LibSupportToolsServerWork` also holds `Support Tools Server Editor`, which
edits the server's git repos and templates directly (it checks the
server's `Version` before every write), and `GitProjects/`, which maps
the `GitProjects` that the server computes from its own clones.

\---

## State

All persistent state — projects, groups, server infos, allow-tool
lists, Git URLs — lives in a single JSON parameters file. The path is
supplied by `ArgumentsAnalyzer` (the `--use` option) and the file is
loaded by `ParametersService` (without `--use` it is searched for in
the current directory and next to the executable).
See [Configuration](configuration.md). The shared part of the file is
also kept on SupportToolsServer (see [Registry layer](#registry-layer));
the file stays the working copy.

\---

## Related

* [Getting Started](getting-started.md) — clone list, build, first run
* [Development](development.md) — code style, analysis, extending the menu
* [Related Projects](related-projects.md) — WebAgent / WebAgentInstaller

