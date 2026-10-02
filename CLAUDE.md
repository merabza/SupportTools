# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SupportTools is a .NET 10 menu-driven console application that orchestrates Git sync, database lifecycle, deployment, project scaffolding, and remote-server operations across a fleet of related .NET projects. Remote work goes through a companion HTTP service called **WebAgent**; all persistent state lives in a single JSON parameters file under the user profile.

Detailed docs live in [docs/en/](docs/en/) — `architecture.md`, `configuration.md`, `development.md`, `getting-started.md`, and `use-cases/`. Prefer reading those over re-deriving facts from the code.

## Build / run

```bash
dotnet build SupportTools.slnx
dotnet run --project SupportTools/SupportTools.csproj
```

Unit tests live in `SupportTools.Tests` (xUnit + Moq; `dotnet test SupportTools.slnx`). Coverage is thin — only a few menu commands — so verification is still mostly manual: build + run + exercise the menu. `Inputer` prompts read the console directly (`Console.ReadKey`) and fail in the test host; to test an interactive command, give it an internal constructor that takes the input functions, as `SaveGitIgnoreAsNewTemplateCliMenuCommand` does (`SupportTools.csproj` already has `InternalsVisibleTo` for the test project). A test class that redirects `Console.Out` must be marked `[Collection(ConsoleCaptureCollection.Name)]` — `Console.SetOut` is process-wide and xUnit runs classes in parallel, so without it console-capturing classes race each other. `stryker-report/` holds git-ignored Stryker mutation-report output; no Stryker config is checked in.

## Critical: sibling-repo layout

`SupportTools.slnx` references **ten external repos as siblings** via relative paths (`../AppCliTools/...`, `../SystemTools/...`, etc.). The build fails immediately if those checkouts are not present alongside this repo. See [docs/en/getting-started.md](docs/en/getting-started.md) for the clone list. The repos are:

`AppCliTools`, `BackendCarcass`, `BackendCarcassShared`, `ConnectionTools`, `DatabaseTools`, `ParametersManagement`, `SupportToolsServerShared`, `SystemTools`, `ToolsManagement`, `WebAgentContracts`.

If a referenced type can't be found, first check whether the sibling repo exists and is on the right branch — don't assume it's a missing using.

## Build is strict — every warning is an error

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `CodeAnalysisTreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisMode=All`, plus `SonarAnalyzer.CSharp` globally. There are **no per-project overrides**. Any analyzer warning, style violation, or nullable-reference warning fails the build. Compiler-implicit usings are **disabled** — every file declares its own `using`s.

Local suppressions use `// ReSharper disable once <rule>` rather than global config. Primary constructors are avoided (look for `// ReSharper disable once ConvertToPrimaryConstructor`). Service classes are `sealed` where practical.

## Package versioning

Central via `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). `.csproj` files use `<PackageReference Include="X" />` with **no version**. To add a package: add `<PackageVersion ... Version="..."/>` to `Directory.Packages.props` *first*, then `<PackageReference Include="..."/>` in the consuming project.

## Architecture in one paragraph

Five layers, bottom-up: (1) data models in `SupportToolsData`/`LibGitData`/`LibTools`; (2) process orchestration in `LibDotnetWork`/`LibGitWork`/`LibNpmWork`; (3) domain logic in `LibDatabaseWork`/`LibCodeGenerator`/`LibAppInstallWork`/`LibSupportToolsServerWork`; (4) high-level workflows in `LibAppProjectCreator`/`LibScaffoldSeeder`; (5) entry point in `SupportTools` (console), with DI registration in `SupportTools/DependencyInjection/SupportToolsServices.cs`. Menu commands are factory-strategy classes auto-discovered through `AddTransientAllStrategies<>` at startup.

## Adding work — the two main patterns

**New menu command:** create `*CliMenuCommandFactoryStrategy` + `*CliMenuCommand` under `SupportTools/Menu/<level>/`, then register the strategy name in the matching list in [SupportTools/Menu/MenuData.cs](SupportTools/Menu/MenuData.cs) (`MainMenu*`, `ProjectGroupSubMenu*`, or `ProjectSubMenu*`). DI picks it up automatically.

**New per-project tool action:** add an enum value to [SupportToolsData/EProjectTools.cs](SupportToolsData/EProjectTools.cs) or [EProjectServerTools.cs](SupportToolsData/EProjectServerTools.cs) (with a Georgian-language comment matching existing style), implement a `*ToolAction` in the right `Lib*` library, and wire it via [SupportTools/ToolCommandFactory.cs](SupportTools/ToolCommandFactory.cs). The factory looks up strategies by `tool.ToString()`, so the strategy's `ToolCommandName` must equal the enum name.

## State and config

All registered projects, servers, git repos, templates, API clients, and connection strings live in one JSON file at a user-profile path supplied by the `--use` argument and loaded by `ParametersService<SupportToolsParameters>`. Top-level shape is `SupportToolsParameters` ([SupportToolsData/Models/SupportToolsParameters.cs](SupportToolsData/Models/SupportToolsParameters.cs)). `Newtonsoft.Json` is used so adding new fields to a model is safe — existing JSON loads fine.

When editing fields: add property → add a `FieldEditor` under `SupportTools/FieldEditors/` → wire into the corresponding `*ParametersEditor` in `SupportTools/ParametersEditors/` or `Cruders/`. Direct JSON edits skip in-app validation.

Every change must end with `IParametersManager.Save` of the **root** object (`parametersManager.Parameters`). Passing a sub-object (e.g. `AppProjectCreatorAllParameters`) overwrites the whole file with it and replaces `IParametersManager.Parameters`, which breaks every later `(SupportToolsParameters)` cast. Cruders built on `SimpleNamesListCruder` / `SimpleNamesWithDescriptionsCruder` (AppCliTools) save only when constructed through their `IParametersManager` overload; the old overloads stay for other applications and save nothing. `ParametersManager.Save` replaces the file atomically (temp file in the same folder + `File.Move`), skips the write when the content is unchanged, and keeps the 10 newest `<file>.yyyyMMdd-HHmmss-fff.bak` copies next to it; manually named backups are never deleted.

## Machine profile and path mapping (central registry)

SupportTools runs on more than one computer: **PAZISI** is the main Windows computer (its folder layout is the canonical one) and **Merinson** is a separate Linux computer that also runs SupportTools. Both had `IsLocal=true` in `Servers` because the file was copied between them; `IsLocal` is per computer and is no longer meant to be shared.

- Machine fields on `SupportToolsParameters`: `MachineName` (optional, read it via `GetMachineNameOrDefault()` → `Environment.MachineName`; `MachineNameFieldEditor` stores null when the offered name of this computer is accepted, so a copied file keeps working), `CurrentMachineServerName` (the `Servers` key that is this computer; null = no such entry; PAZISI on PAZISI, Merinson on Merinson) and `PathMappings` (`List<PathMappingModel { CanonicalPrefix, LocalPrefix }>`). They are edited at the top of "Support Tools Parameters Editor"; the Path Mappings list has "Suggest Path Mappings...", which collects drive + first-folder roots (`D:\1WorkDotnet`) from the README §4.4 path fields (project paths, ServerInfo appsettings, ProjectCreator folders, local FileStorage paths) and asks for a local prefix per root.
- `ServerDataModel.IsLocal` is computed: `Program.cs` calls `ServersIsLocalCalculator.Recalculate` right after the parameters load, before the menu and `--run`. With `CurrentMachineServerName` set, only that server (case-insensitive) is local; when it is empty the stored values stay (old files keep working). The `CurrentMachineServerName` editor recalculates at once, and so does `ServerDataCruder` after adding or renaming a server; while the name is set, the `Servers` editor hides `IsLocal`. Code that replaces `Servers` (registry pull) must call it again.
- Path rule — `LibSupportToolsServerWork/Registry/Paths/PathMapper`: the canonical form is the Windows absolute path as on PAZISI. `ToLocal` / `ToCanonical` swap the longest matching prefix; a prefix matches only on a separator boundary (`D:\1WorkDotnet` ≠ `D:\1WorkDotnetX`), case-insensitively, ignoring trailing separators. On non-Windows (`Path.DirectorySeparatorChar == '/'`) the rest of the path also swaps `\` ↔ `/`. An unmatched path is returned unchanged; on Linux a Windows-rooted path in `ToLocal` or a `/`-rooted path in `ToCanonical` is also added to `Issues` (the sync command shows them). Relative paths (`GitProjectFolderName`) use `NormalizeRelativeToLocal` / `NormalizeRelativeToCanonical` (separators only). null/empty pass through. The internal constructor takes the separator, so tests cover both modes on Windows.
- Fields that never sync are listed in `LibSupportToolsServerWork/Registry/MachineLocalFields.cs`; add every new machine-local field there.

## Registry sync engine (central registry)

`LibSupportToolsServerWork/Registry/Sync/` is the 3-way, per-record sync engine (local / server / last successful sync, with the server's `Version`; plan README §4.3 in SupportToolsServer's `docs/CentralRegistry`). It has no UI and no concrete collections: the adapters (C3/C4), the "Sync Registry" command (C5) and auto-sync (D2) build on it. Tests in `SupportTools.Tests/Registry/Sync/` use `FakeRegistrySyncAdapter`, an in-memory server that follows the version rules below.

- **State**: `SupportToolsParameters.RegistrySyncState` (`RegistrySyncStateModel`, a machine-local field) is saved with the data by the same `Save`. Per collection (`Collections[CollectionName]`): `Records` (key → server `Version` + hash of the local contract at the last successful sync) and `ExcludedKeys` (never pushed or pulled, e.g. a database connection that differs on Linux); plus `LastSyncUtc` (set by an execution that changed data or state). The dictionaries and the set are get-only and created with `OrdinalIgnoreCase`: Newtonsoft fills those instances on load, so keys stay case-insensitive (G8) and the instances are never replaced.
- **Adapter** (`IRegistrySyncAdapter`, one per collection; the contract is the server's `Sts…DataModel`, an opaque `object` to the engine):
  - `CollectionName`: the state key, so never rename it. `Order`: dependency order; a referenced collection has a lower order than the one that references it, and Projects is last.
  - `Normalize(contract)`: `""` → null, sets sorted with `OrdinalIgnoreCase`, etc. It must be idempotent; the engine checks that normalizing again keeps the hash.
  - `GetLocalRecords()`: key → contract with canonical paths and without machine fields. `GetServerRecords(ct)`: key → `RegistryServerRecord(contract, Version)`.
  - `Upsert(key, contract, expectedVersion, ct)` returns `Result<int>` with the new version (expected version 0 = create). `Delete(key, expectedVersion, ct)`. Errors carry the server's code.
  - `ApplyLocal(key, contract)` merges into the local model in place: it keeps machine fields and fields the contract lacks and never replaces dictionary instances. The key is the local spelling when the record exists locally. `RemoveLocal(key)`. Adapters never save.
- **Hash** (`RegistryContractHasher`): SHA-256, upper-case hex, of canonical JSON. The root `Version` is removed; nulls and default values are omitted, so a new, still-empty contract field keeps the old hashes; object properties and dictionary keys are sorted ordinally; list order is kept. The serializer settings live only there. Changing them makes every stored hash look locally changed, and a golden-value test guards against that.
- **Planner** (`RegistrySyncPlanner.CreatePlan`, a pure function):
  - A key in `ExcludedKeys` → Skipped.
  - Record on both sides: equal hashes → InSync (also when only one side changed); not in the state → Conflict(FirstSyncDiffers); otherwise localChanged = hash ≠ state.Hash and serverChanged = Version ≠ state.Version decide between InSync / Pull(Update) / Push(Update) / Conflict(BothChanged).
  - Local only: not in the state → Push(Add); unchanged → Pull(Delete); changed → Conflict(DeletedOnServer).
  - Server only: not in the state → Pull(Add); unchanged → Push(Delete); changed → Conflict(DeletedLocally).
  - Only in the state (deleted on both sides) → InSync, and the executor forgets it.
  - Plan items are ordered by `Order`, collection and key, and carry the local spelling of the key when the record exists locally.
- **Engine** (`RegistrySyncEngine(adapters, parametersManager)`):
  - `CreatePlan(ct)` reads, normalizes and hashes every adapter's records and changes nothing. It fails on a server read error (returned unchanged, e.g. `ApiRequestFailed` when offline), on keys that differ only by case (`DuplicateKeys`) and on unstable normalization (`NormalizationIsNotStable`).
  - `Execute(plan, selection, ct)` runs `RegistrySyncExecutor`. `RegistrySyncSelection` has `IncludePulls`, `IncludePushes` and `ConflictResolutions` (plan item → Local / Server / Skip; a missing item = Skip), with the presets `AllNonConflicting`, `PullOnly` and `PushOnly`.
  - Order: server upserts by ascending `Order`, then server deletes by descending order, then `ApplyLocal` ascending, then `RemoveLocal` descending, then one `IParametersManager.Save` of the root, only if local data or state changed.
  - Each successful operation updates the record's state. After the local operations each touched adapter's local records are read again, and the state stores the hash of what the adapter now returns. A pulled record that does not show up, or a removed one that does not disappear, is `Failed` and gets no state, so the next sync cannot mistake it for a local delete.
  - A plan is executed once; build a new plan afterwards. Cancellation propagates without saving; the next run recovers, because equal content on both sides plans as InSync.
- **Server errors** (`RegistrySyncServerErrorCodes`, compared with `Error.Code`):
  - `ConcurrencyConflict` → outcome Conflict, and the rest continues.
  - `RecordWithNameNotFound` → Conflict on an upsert; on a delete the record is already gone, so the outcome is Done.
  - `ApiRequestFailed` → Failed, and every remaining server operation is NotExecuted. Completed operations stay in the state and the local operations still run.
  - Any other code (`RecordIsInUse`, validation, 5xx) → Failed, and the rest continues.
  - The first two are the names of B1's error factories in `SupportToolsServerApiClientErrors`; they are literals here until B1 adds them.
- **Report** (`RegistrySyncReport`): one `RegistrySyncReportItem(PlanItem, Outcome, Error)` per plan item, with outcome None, NotSelected, Done, Conflict, Failed or NotExecuted. It also has `TransportError`, `Changed` and `Saved`.

## Conventions to match

- Code comments are predominantly in Georgian — match the language of nearby comments rather than translating.
- Use `StShared.WriteException` / `StShared.WriteErrorLine` (from `SystemTools.SystemToolsShared`) for diagnostics rather than `Console.WriteLine`.
- `Serilog` is the configured logger; logs land under the parameters-file `LogFolder`.
- Local-vs-remote routing on a `ServerDataModel` is controlled by `IsLocal`. Remote = HTTP to WebAgent.

## What not to touch without good reason

- `Directory.Build.props` / `Directory.Packages.props` — global; affects every project.
- The strategy auto-registration calls in `SupportToolsServices.cs` — `AddTransientAllStrategies` uses `Assembly` markers; one marker per assembly is sufficient and adding another won't help.
- `.editorconfig` — 16 KB of enforced rules. If a rule is "wrong," prefer a local `// ReSharper disable once` over editing the file.
