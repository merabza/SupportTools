# Development

How to work on SupportTools itself: build settings, code style, and the
patterns for adding new functionality.

## Build settings

All projects inherit from `Directory.Build.props`:

```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>disable</ImplicitUsings>
<Nullable>enable</Nullable>

<AnalysisLevel>latest</AnalysisLevel>
<AnalysisMode>All</AnalysisMode>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

Consequences:

* Implicit `using`s are off — every file declares its own
* Nullable reference types are on
* Every analyzer warning is a build error
* Every code-style violation (from `.editorconfig`) is a build error

`SonarAnalyzer.CSharp` runs across the whole solution. There are **no
project-specific overrides** — the same rules apply everywhere.

\---

## Package management

NuGet versions are pinned centrally in `Directory.Packages.props`
(`ManagePackageVersionsCentrally = true`). Individual `.csproj` files
list `<PackageReference Include="X" />` without a version, and the
version comes from the central props file.

When adding a new package:

1. Add a `<PackageVersion Include="Foo" Version="x.y.z" />` to
`Directory.Packages.props`
2. Add `<PackageReference Include="Foo" />` to the consuming
`.csproj`

\---

## Code style

`.editorconfig` is the source of truth. The build enforces it. Key
project-wide conventions visible in the source:

* `// ReSharper disable once <rule>` is used to silence ReSharper
locally rather than disabling globally
* Primary constructors are avoided (the codebase has
`// ReSharper disable once ConvertToPrimaryConstructor` markers)
* Service classes are `sealed` where practical
* `using` directives are sorted (the `dotnet format` step in
`JetBrainsCleanupCode` reorders them)

\---

## Adding a new menu command

A command is a factory strategy plus a command class. Walk-through:

1. **Pick a location** — under `SupportTools/Menu/`, in the folder
matching the menu level (`ProjectGroupsList`, `ProjectsList`, etc.)
2. **Create a `\*CliMenuCommandFactoryStrategy`** — registers the
command with the menu builder
3. **Create the `\*CliMenuCommand`** — implements the actual action,
typically delegating to a `\*ToolAction` class in the appropriate
`Lib\*` library
4. **Register the strategy** — add `nameof(MyNewCommandFactoryStrategy)`
to the relevant list in `SupportTools/Menu/MenuData.cs`
* `MainMenuCommandFactoryStrategyNames` for top-level
* `ProjectGroupSubMenuCommandFactoryStrategyNames` for group-level
* `ProjectSubMenuCommandFactoryStrategyNames` for project-level

The DI container picks up factory strategies automatically through
`SupportTools.Application`'s service registration.

\---

## Adding a new per-project tool action

Per-project tools (the ones listed in `EProjectTools` /
`EProjectServerTools`) follow a different pattern:

1. **Add an enum value** to `SupportToolsData/EProjectTools.cs` or
`EProjectServerTools.cs`, with a Georgian comment summarizing the
purpose (matches existing style)
2. **Implement a `\*ToolAction`** in the relevant `Lib\*` library
3. **Wire the action** to the enum in `ToolCommandFactory.cs`
(`SupportTools/ToolCommandFactory.cs`)
4. **Expose the tool** via `SelectProjectAllowTools` so users can
enable it per project

\---

## Adding a field to a configuration model

1. **Add the property** to the relevant model in
`SupportToolsData/Models/`
2. **Add a `FieldEditor`** under `SupportTools/FieldEditors/` for
in-app editing
3. **Wire the editor** into the corresponding `\*ParametersEditor`
under `SupportTools/ParametersEditors/` or `Cruders/`
4. **Run** — existing JSON files load fine because Newtonsoft.Json
ignores missing properties and uses defaults for new ones

\---

## Testing

There's no integrated test project in this repo. The `stryker-report/`
folder exists but is empty and not wired into any pipeline (see
[Code Quality](use-cases/code-quality.md)).

Verification is currently manual: build + run + exercise the menu.

\---

## Debugging

* The application loads slowly on first hit because it reads the full
parameters JSON. For faster iteration, point `ArgumentsParser` at a
smaller scratch JSON file with one project.
* `Serilog` is the logger; check the path under `LogFolder` in your
parameters file.
* `StShared.WriteException` (from `SystemTools.SystemToolsShared`)
formats exceptions consistently — use it in new code rather than
raw `Console.WriteLine`.

\---

## Related

* [Architecture](architecture.md) — module layering before adding to
the right layer
* [Configuration](configuration.md) — model field reference
* [Code Quality](use-cases/code-quality.md) — analyzers and cleanup

