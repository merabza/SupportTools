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
|`LibSupportToolsServerWork`|Remote server operations via WebAgent|LibDotnetWork, LibGitData, SupportToolsData|
|`LibAppProjectCreator`|Project scaffolding from templates|LibAppInstallWork, LibGitWork, LibNpmWork|
|`LibScaffoldSeeder`|Scaffold + seed code from existing databases|LibAppProjectCreator, LibGitWork|
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
         │ LibAppProjectCreator      LibScaffoldSeeder              │
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
orchestrator in `LibAppInstallWork` or `LibScaffoldSeeder`, which
delegates to `LibDotnetWork`/`LibGitWork`/`LibNpmWork` for process work,
which uses `LibGitData`/`SupportToolsData` for typed state.

\---

## External repositories the solution depends on

These are sibling Git checkouts referenced via relative paths in
`SupportTools.slnx`. They are maintained as separate repositories:

|Repo|Used for|
|-|-|
|`AppCliTools`|CLI menu, parameter editing, data input, seed-code creation|
|`BackendCarcass`|Shared backend framework projects|
|`BackendCarcassShared`|Carcass contracts|
|`ConnectionTools`|FTP/connection-pool utilities|
|`DatabaseTools`|SQL Server, SQLite, OleDb adapters|
|`ParametersManagement`|API client, file, and database parameter management|
|`SupportToolsServerShared`|Contracts shared with SupportToolsServer|
|`SystemTools`|System abstractions, background tasks, database tools shared types|
|`ToolsManagement`|API clients, compression, databases, file managers, installer|
|`WebAgentContracts`|Contracts for the WebAgent API (databases, projects)|

\---

## Entry point

`SupportTools/Program.cs` does the standard CLI bootstrap:

1. `ArgumentsParser<SupportToolsParameters>` reads CLI args
2. `ServiceCollection.AddServices(...)` (in
`SupportTools.Application`) wires up DI
3. `CliAppLoopParameters.Create<Program>(...)` builds the menu loop
4. `CliAppLoop.Run()` runs until the user exits

Menu commands are registered as factory strategies in
`SupportTools/Menu/`. The main menu is assembled in
`SupportToolsMenuBuilder` from `MenuData.MainMenuCommandFactoryStrategyNames`.

\---

## State

All persistent state — projects, groups, server infos, allow-tool
lists, Git URLs — lives in a single JSON parameters file. The path is
supplied by `ArgumentsParser` (defaults to a user-profile location).
See [Configuration](configuration.md).

\---

## Related

* [Getting Started](getting-started.md) — clone list, build, first run
* [Development](development.md) — code style, analysis, extending the menu
* [Related Projects](related-projects.md) — WebAgent / WebAgentInstaller

