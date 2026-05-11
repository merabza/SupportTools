# SupportTools

Modular CLI application for managing .NET multi-project environments:
Git synchronization, database lifecycle, scaffolding, deployment, and
remote server operations.

[ქართულად](README.ka.md)

## Overview

SupportTools is a .NET 10 console application that orchestrates the
repetitive operational work surrounding a collection of related .NET
projects. Instead of `cd`-ing into each repo to pull, building each
service individually, or hand-encoding `appsettings.json` for each
environment, you do all of it from one menu-driven CLI.

The tool runs on any platform that supports .NET. Remote server work is
mediated by a companion service called WebAgent, so the same SupportTools
binary on a developer machine can manage installs on Linux, Windows, or
any host running WebAgent.

State — projects, groups, server entries, Git URLs — lives in a single
JSON parameters file under your user profile.

## Capabilities

* **Git** — sync many repositories at once (all projects, one group, or
one project); manage a project's list of Git URLs
* **Database** — create or drop a dev database from EF migrations;
correct post-migration schema; analyze schema; scaffold from a prod
copy; extract data to JSON; seed a dev database from JSON
* **Deployment** — encode `appsettings.json`, publish a build, install
or remove a program on a server, start/stop/version-check services
(locally or remotely via WebAgent)
* **Project creation** — bootstrap new Console/API/Razor solutions
from templates; import/export project configurations; bulk-update NuGet
packages across projects
* **Code quality** — run `jb cleanupcode` against a solution; generate
API-route constant classes from contract metadata

## Documentation

* [Getting Started](docs/en/getting-started.md) — prerequisites, clone,
build, first run
* [Architecture](docs/en/architecture.md) — module layering and
dependencies
* [Configuration](docs/en/configuration.md) — parameters file structure
* [Development](docs/en/development.md) — code style, analyzers,
extending the menu
* [Related Projects](docs/en/related-projects.md) — WebAgent and
WebAgentInstaller

### Use cases

|If you need to...|See|
|-|-|
|Sync Git repositories|[Git Operations](docs/en/use-cases/git-operations.md)|
|Create, drop, or analyze a database|[Database Operations](docs/en/use-cases/database-operations.md)|
|Deploy a build, encode app settings, manage a service|[Deployment](docs/en/use-cases/deployment.md)|
|Create a new project, import, or update packages|[Project Creation](docs/en/use-cases/project-creation.md)|
|Run code cleanup or generate API routes|[Code Quality](docs/en/use-cases/code-quality.md)|

## Quick start

Prerequisites: .NET 10 SDK, Git.

Clone the ten sibling repositories that SupportTools depends on (see
[Getting Started](docs/en/getting-started.md) for the full script), then:

```bash
dotnet build SupportTools.slnx
dotnet run --project SupportTools/SupportTools.csproj
```

## License

MIT — see [LICENSE](LICENSE).

