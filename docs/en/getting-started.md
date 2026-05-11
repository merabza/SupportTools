# Getting Started

How to clone all required repositories, build, and run SupportTools.

## Prerequisites

* .NET 10 SDK
* Git (with SSH access to the source repos)
* Optional: JetBrains ReSharper Command Line Tools — only needed for
the `JetBrainsCleanupCode` command

\---

## Clone all required repositories

SupportTools depends on ten sibling repositories. Create a parent
folder and clone them next to each other.

```bash
mkdir SupportTools
cd SupportTools
git clone git@github.com:merabza/AppCliTools.git              AppCliTools
git clone git@github.com:merabza/BackendCarcass.git           BackendCarcass
git clone git@github.com:merabza/BackendCarcassShared.git     BackendCarcassShared
git clone git@github.com:merabza/ConnectionTools.git          ConnectionTools
git clone git@github.com:merabza/DatabaseTools.git            DatabaseTools
git clone git@github.com:merabza/ParametersManagement.git     ParametersManagement
git clone git@github.com:merabza/SupportTools.git             SupportTools
git clone git@github.com:merabza/SupportToolsServerShared.git SupportToolsServerShared
git clone git@github.com:merabza/SystemTools.git              SystemTools
git clone git@github.com:merabza/ToolsManagement.git          ToolsManagement
git clone git@github.com:merabza/WebAgentContracts.git        WebAgentContracts
cd ..
```

After this you should have a layout like:

```
SupportTools/
  ├── AppCliTools/
  ├── BackendCarcass/
  ├── BackendCarcassShared/
  ├── ConnectionTools/
  ├── DatabaseTools/
  ├── ParametersManagement/
  ├── SupportTools/                 ← this repo
  │   └── SupportTools.slnx
  ├── SupportToolsServerShared/
  ├── SystemTools/
  ├── ToolsManagement/
  └── WebAgentContracts/
```

The solution references each sibling with relative paths
(`../AppCliTools/...` etc.), so this layout is required.

\---

## Build

From the `SupportTools/` repo:

```bash
dotnet build SupportTools.slnx
```

The build enforces `TreatWarningsAsErrors`, `AnalysisMode=All`, and
`EnforceCodeStyleInBuild` (see [Development](development.md)). Any
warning fails the build.

\---

## Run

```bash
dotnet run --project SupportTools/SupportTools.csproj
```

Or run the executable directly after building:

```bash
./SupportTools/bin/Debug/net10.0/SupportTools.exe
```

The application starts an interactive CLI menu. The main menu is
assembled in `SupportToolsMenuBuilder` and listed in
`SupportTools/Menu/MenuData.cs` (`MainMenuCommandFactoryStrategyNames`).

\---

## First run

On first run, SupportTools creates a parameters JSON file in your user
profile and shows the main menu with no projects registered. From here:

1. **Edit basic parameters** —
`SupportToolsParametersEditorListCliMenuCommandFactoryStrategy`. Set
global paths (work folder, security folder, code-generate test folder).
2. **Create a project group** — open `Project Groups List`, add a group.
3. **Add a project** — either:
* `Create New Project` (template-driven, see
[Project Creation](use-cases/project-creation.md)), or
* `Import Project` if you have an exported JSON from another machine.
4. **Configure Git repos** for the project — see
[Git Operations](use-cases/git-operations.md).
5. **Sync** — `Sync All Projects All Gits V2` clones everything.

For the full state-file structure, see [Configuration](configuration.md).

\---

## Verifying the install

A working install passes these checks:

* `dotnet build SupportTools.slnx` finishes with no errors and no
warnings
* Running the executable shows a main menu with `Project Groups List`,
`Sync All Projects All Gits V2`, etc.
* Selecting `Recent Commands List` shows an empty list (no history yet)
on a fresh install

\---

## Related

* [Architecture](architecture.md) — what the modules do
* [Configuration](configuration.md) — parameters file layout
* [Development](development.md) — code style, analyzers, adding commands

