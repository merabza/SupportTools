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
  - The first two are `nameof` B1's error factories in `SupportToolsServerApiClientErrors`; the adapters that check the version themselves (see below) create their errors with the same factories.
- **Report** (`RegistrySyncReport`): one `RegistrySyncReportItem(PlanItem, Outcome, Error)` per plan item, with outcome None, NotSelected, Done, Conflict, Failed or NotExecuted. It also has `TransportError`, `Changed` and `Saved`.

## Registry adapters (central registry)

`LibSupportToolsServerWork/Registry/Adapters/` holds one `IRegistrySyncAdapter` per shared collection (C3, and C4 for Projects), `Registry/Mappers/` the pure mappers. Tests: `SupportTools.Tests/Registry/Mappers/` (round trips, hash stability, Linux paths) and `Registry/Adapters/`, which run the real `SupportToolsServerApiClient` against `FakeSupportToolsServer`, an in-memory server with B1's version rules and the old git endpoints; `RegistrySyncAdaptersEndToEndTests` seeds it from a Windows computer and pulls everything to a Linux one through the engine. A DELETE starts the message hub, which writes to the console, so the adapter test classes are in `ConsoleCaptureCollection`.

- **Factory**: `RegistrySyncAdapterFactory.CreateAdapters(apiClient, parameters, pathMapper, warnings)` builds all of them; the caller (C5) creates the `PathMapper` from `parameters.PathMappings` and shows `pathMapper.Issues` and `RegistrySyncWarnings.Items` (collection, key or null, message; never a secret value). `RegistryCollections` holds the names (state keys, never rename) and orders: Environments 10, RunTimes 20, NpmPackages 30, ReactAppTemplates 40, DotnetTools 50, SmartSchemas 60, FileStorages 70, ApiClients 80, DatabaseServerConnections 90, Servers 100, GitIgnorePatterns 110, Gits 120, EditorConfigPatterns 130, ProjectTemplates 140, GlobalSettings 150, ProjectCreatorSettings 160, Projects 170. The Projects adapter gets the Gits, NpmPackages, EditorConfigPatterns and DatabaseServerConnections adapters as `ProjectReferenceServerKeys`: every `RegistrySyncAdapter` exposes the keys of its last server read (`IRegistryServerKeys`), and the engine reads the adapters in `Order`, so Projects sees them already read.
- **Mappers** (`<X>Mapper`): `ToContract(key, local[, pathMapper])`, `ApplyToLocal(contract, existing[, pathMapper])` that sets only the contract's fields (or `ToLocal` for strings and the init-only `SmartSchema`), and `Normalize` (`ContractNormalization`: `EmptyToNull`, `OrderByName` for child lists, `EnumName` for the canonical spelling of enum names, `UpperDriveLetter` for canonical paths, because the real data holds `d:\` paths and a Linux `ToCanonical` gives back the rule's `D:\`; the rest of a path keeps its case). Required contract strings are not nulled. Machine fields stay out (`MachineLocalFieldsContractTests`).
- **Bases**:
  - `RegistrySyncAdapter<TContract>`: the casts and the server records. A key is left out on both sides (no push, pull or delete) when `IsSynced(key)` refuses it, or when `FindUnsupportedField` finds a server value this client cannot store (an unknown `EPeriodType`, `EDatabaseProvider` or `ESupportProjectType` name), which also adds a warning: uploading the local record would lose the server's value.
  - `DictionaryRegistrySyncAdapter<TLocal, TContract>`: `ApplyLocal` passes the existing record to `ToLocal`, which updates it in place.
  - `SingletonRegistrySyncAdapter<TContract>` (GlobalSettings, key `Global`; ProjectCreatorSettings, key `ProjectCreator`, B5's `RecordName`): an empty contract (hash of `new TContract()`) counts as missing on both sides, so a new computer pulls it without a conflict (the user's choice; SupportToolsServer's CLAUDE.md, Singletons, says the same). Delete writes an empty contract with the expected version, `RemoveLocal` clears the shared fields, and a create (expected version 0) over a server record that was emptied uses the version read with the plan.
  - `TemplateFilesRegistrySyncAdapter<TContract>` (GitIgnorePatterns, EditorConfigPatterns): a record is a list name plus the content of `{folder}\{name}.gitignore|.editorconfig`. A listed name without its file, or every name when the folder is not set, is left out on both sides with a warning. `ApplyLocal` writes the file (creating the folder) and adds the name; `RemoveLocal` only removes the name and keeps the file (the user's choice).
- **Old git endpoints** (Gits, GitIgnorePatterns, EditorConfigPatterns): B1 left `updategitrepo`, `syncup…/{merge}` and the old deletes without a version check, so `UpsertCheckingVersionOnClient` / `DeleteCheckingVersionOnClient` compare the version in the server's list first (B1's rule, with `ConcurrencyConflict` / `RecordWithNameNotFound`), write (a template goes as a one-row `SyncUp` with `merge=true`), and read the new version from the list. The old not-found codes (`GitWithKeyNotFound`, `…FileTypeWithNameNotFound`) become `RecordWithNameNotFound`. A change by another computer between the check and the write can still be overwritten (the user accepted that small window).
- **Collection rules**: DotnetTools sync only `PackageId`, `MaxVersion`, `Description`. ApiClients leave out the one named by `SupportToolsServerWebApiClientName` (bootstrap, G6) on both sides. Servers send no `IsLocal` and call `ServersIsLocalCalculator.Recalculate` after `ApplyLocal`. `FileStoragePath` is mapped only when it is a path (`PathMapper.IsWindowsRooted` or `FileStat.IsFileSchema`), not a URL. ProjectCreatorSettings map `ProjectsFolderPathReal` and `SecretsFolderPathReal` and create `AppProjectCreatorAllParameters` when it is null; so do the ProjectTemplates (its `Templates`). Gits normalize only the separators of `GitProjectFolderName`. DatabaseServerConnections replace the folders sets inside the existing dictionary.
- **Projects** (C4; `ProjectMapper`, `ServerInfoMapper`, `DatabaseParametersMapper`): one aggregate per project with its ServerInfos (G7). The 15 project paths and the two AppSettings files of a ServerInfo go through the `PathMapper`; `KeyGuidPart` travels (G2) and is never printed; `RouteClassModel.Version` is the contract's `ApiVersion`; `ScaffoldSeederGitProjectNames` travels as it is. Enum names (`EProjectType`, `EProjectTools`, `EProjectServerTools`, `EHttpMethod`, `EEndpointType`, `EDatabaseRecoveryModel`, `EBackupType`) unknown to this client make the server project `FindUnsupportedField`, so it stays out on both sides with a warning until SupportTools is updated (a pull would lose the name, a push would delete it on the server).
  - ServerInfos: the natural key is (ServerName, EnvironmentName), ignoring case. `ApplyLocal` replaces the whole set (G7) inside the existing dictionary, keeps the local key (a GUID or `Server|Env`) of a record it finds by the natural key, gives a new one `"{ServerName}|{EnvironmentName}"` (a new GUID when another record holds that key) and removes the rest.
  - `ApplyLocal` updates the existing `ProjectModel` (and `ServerInfoModel`) in place when the contract changes none of its `init`-only fields (the database parameters only in content); otherwise it creates a new object that takes over the old one's lists, dictionaries and database parameters, and puts it into the dictionary (safe, because a pull runs before the menu is rebuilt). Every field of the aggregate is in the contract today; a new local-only field must be kept there too (`ProjectMapperTests.LocalModels_…` lists the fields the contract lacks).
  - Warnings (C5 shows them): a git, npm package, `.editorconfig` template or DB connection that is neither local nor among the server keys; a path without a canonical form; repeated values in a set, in the endpoint or route class names or in the ServerInfo natural keys, ignoring case. A project with repeated values is not sent: `Upsert` returns `RegistrySyncErrors.LocalRecordIsInvalid` (outcome Failed), while a pull still works. Local project keys that differ only by case are left out on both sides with a warning (`IsSynced`), so the engine does not stop the whole plan with `DuplicateKeys`.

## Conventions to match

- Code comments are predominantly in Georgian — match the language of nearby comments rather than translating.
- Use `StShared.WriteException` / `StShared.WriteErrorLine` (from `SystemTools.SystemToolsShared`) for diagnostics rather than `Console.WriteLine`.
- `Serilog` is the configured logger; logs land under the parameters-file `LogFolder`.
- Local-vs-remote routing on a `ServerDataModel` is controlled by `IsLocal`. Remote = HTTP to WebAgent.

## What not to touch without good reason

- `Directory.Build.props` / `Directory.Packages.props` — global; affects every project.
- The strategy auto-registration calls in `SupportToolsServices.cs` — `AddTransientAllStrategies` uses `Assembly` markers; one marker per assembly is sufficient and adding another won't help.
- `.editorconfig` — 16 KB of enforced rules. If a rule is "wrong," prefer a local `// ReSharper disable once` over editing the file.
