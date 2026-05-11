# Code Quality

Commands that run static analysis or generate consistent code across
projects.

## Quick Reference

|Command|Purpose|
|-|-|
|`JetBrainsCleanupCode`|Run ReSharper's `jb cleanupcode` over a project's solution|
|`GenerateApiRoutes`|Generate C# constants for API routes from contract metadata|

\---

## If you need to format and clean up a solution

Use before a commit, after a large refactor, or to enforce house style
on imported code.

**Steps**

1. `Project Tools List` → `JetBrainsCleanupCode`

**What it does**

* Invokes JetBrains ReSharper's command-line code-cleanup utility
(`jb cleanupcode`) against the project's `SolutionFileName`
* Uses ReSharper defaults — cleanup profiles are configured at the IDE
level, not in this tool

**Prerequisites**

* JetBrains ReSharper Command Line Tools installed and on `PATH`
(`jb`)
* The project's `SolutionFileName` is set in its parameters
* The `.sln` file exists on disk

**Expected output**

* Files in the solution are reformatted and re-organized per the
ReSharper rules in effect
* No code change reports in console — re-run `git status` to see what
was modified

**Common pitfalls**

* **`SolutionFileName` missing.** The command logs an error and stops.
Configure the field in project parameters first.
* **`.sln` not on disk.** The tool checks for the file before running
`jb` and aborts if missing — usually a sign the repo wasn't synced.
* **No cleanup profile defaults.** Without an explicit profile, `jb`
applies the default rules, which may differ from what the team uses
inside the IDE. If output looks wrong, supply a `--profile=...` rule
manually (the tool does not pass one).

\---

## If you need to regenerate API route constants

Use after adding or renaming controllers, or when an API contract
project drifts from the routes that consume it.

**Steps**

1. `Project Tools List` → `GenerateApiRoutes`

**What it does**

* Reads the project's `ApiContractsProjectName`, `RouteClasses`
dictionary, and `CodeGenerateTestFolder`
* Syncs the relevant Git repository (cleanly checks it out)
* Generates one `{ProjectName}ApiRoutes.cs` per registered route class
under `{ApiContractsProjectFolder}/{Version}/Routes/`
* Each generated class contains `Root` and `Version` constants for the
HTTP routes

**Prerequisites**

* `ApiContractsProjectName` set in SupportTools parameters for the
project
* `RouteClasses` dictionary populated (maps logical group → class name)
* `CodeGenerateTestFolder` set globally in SupportTools parameters
* The contracts project's Git repo is reachable

**Expected output**

* Generated `.cs` files under the contracts project's `Routes/` folder
* Folder structure is created if it doesn't exist

**Common pitfalls**

* **`CodeGenerateTestFolder` unset.** The command fails before
producing any output. Configure the global parameter first.
* **Git checkout fails.** If the underlying sync errors out, generation
silently stops. Check the console output for sync errors, and verify
the repo is reachable before retrying.
* **Partial output on failure.** If the contracts project can't be
located inside the synced repo, the tool may have already created
empty `Routes/` directories. Those directories are not cleaned up —
remove them by hand if they're noise.

\---

## Static analysis (always on, not a command)

`Directory.Build.props` enables the following globally; you don't run
it, you respect it:

* `SonarAnalyzer.CSharp` — active C# analyzer
* `AnalysisLevel = latest`, `AnalysisMode = All`
* `TreatWarningsAsErrors = true`,
`CodeAnalysisTreatWarningsAsErrors = true`
* `EnforceCodeStyleInBuild = true`

Result: any compiler warning, analyzer warning, or style violation
fails the build. There are no project-specific overrides — all projects
inherit this configuration.

See [Development](../development.md) for the workflow implications.

\---

## Mutation testing

The `stryker-report/` directory exists in the repo but is empty.
No `stryker-config.json` is checked in, no Stryker packages are
referenced, and the application does not invoke Stryker. Mutation
testing is **not** part of the current pipeline.

\---

## Related

* [Architecture](../architecture.md) — `LibCodeGenerator`
* [Development](../development.md) — build flags, code style enforcement
* [Configuration](../configuration.md) — `SolutionFileName`,
`ApiContractsProjectName`, `RouteClasses`, `CodeGenerateTestFolder`

