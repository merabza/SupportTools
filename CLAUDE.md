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

There is **no test project and no test runner** in this repo. The `stryker-report/` directory is empty and unwired. Verification is manual: build + run + exercise the menu.

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

Five layers, bottom-up: (1) data models in `SupportToolsData`/`LibGitData`/`LibTools`; (2) process orchestration in `LibDotnetWork`/`LibGitWork`/`LibNpmWork`; (3) domain logic in `LibDatabaseWork`/`LibCodeGenerator`/`LibAppInstallWork`/`LibSupportToolsServerWork`; (4) high-level workflows in `LibAppProjectCreator`/`LibScaffoldSeeder`; (5) entry point in `SupportTools` (console) plus `SupportTools.Application` (currently a near-empty placeholder — DI actually lives in `SupportTools/DependencyInjection/SupportToolsServices.cs`). Menu commands are factory-strategy classes auto-discovered through `AddTransientAllStrategies<>` at startup.

## Adding work — the two main patterns

**New menu command:** create `*CliMenuCommandFactoryStrategy` + `*CliMenuCommand` under `SupportTools/Menu/<level>/`, then register the strategy name in the matching list in [SupportTools/Menu/MenuData.cs](SupportTools/Menu/MenuData.cs) (`MainMenu*`, `ProjectGroupSubMenu*`, or `ProjectSubMenu*`). DI picks it up automatically.

**New per-project tool action:** add an enum value to [SupportToolsData/EProjectTools.cs](SupportToolsData/EProjectTools.cs) or [EProjectServerTools.cs](SupportToolsData/EProjectServerTools.cs) (with a Georgian-language comment matching existing style), implement a `*ToolAction` in the right `Lib*` library, and wire it via [SupportTools/ToolCommandFactory.cs](SupportTools/ToolCommandFactory.cs). The factory looks up strategies by `tool.ToString()`, so the strategy's `ToolCommandName` must equal the enum name.

## State and config

All registered projects, servers, git repos, templates, API clients, and connection strings live in one JSON file at a user-profile path supplied by `ArgumentsParser<SupportToolsParameters>`. Top-level shape is `SupportToolsParameters` ([SupportToolsData/Models/SupportToolsParameters.cs](SupportToolsData/Models/SupportToolsParameters.cs)). `Newtonsoft.Json` is used so adding new fields to a model is safe — existing JSON loads fine.

When editing fields: add property → add a `FieldEditor` under `SupportTools/FieldEditors/` → wire into the corresponding `*ParametersEditor` in `SupportTools/ParametersEditors/` or `Cruders/`. Direct JSON edits skip in-app validation.

## Conventions to match

- Code comments are predominantly in Georgian — match the language of nearby comments rather than translating.
- Use `StShared.WriteException` / `StShared.WriteErrorLine` (from `SystemTools.SystemToolsShared`) for diagnostics rather than `Console.WriteLine`.
- `Serilog` is the configured logger; logs land under the parameters-file `LogFolder`.
- Local-vs-remote routing on a `ServerDataModel` is controlled by `IsLocal`. Remote = HTTP to WebAgent.

## What not to touch without good reason

- `Directory.Build.props` / `Directory.Packages.props` — global; affects every project.
- The strategy auto-registration calls in `SupportToolsServices.cs` — `AddTransientAllStrategies` uses `Assembly` markers; one marker per assembly is sufficient and adding another won't help.
- `.editorconfig` — 16 KB of enforced rules. If a rule is "wrong," prefer a local `// ReSharper disable once` over editing the file.
