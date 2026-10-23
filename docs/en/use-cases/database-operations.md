# Database Operations

Database lifecycle commands for projects with an attached database.
All commands are reached from `Main Menu → Project Groups List → (group) → Projects List → (project) → Project Tools List`, after the relevant
tools have been enabled via `Select Project Allow Tools`.

These commands assume the project has valid database connection
parameters configured (see [Configuration](../configuration.md)).

## Quick Reference

|Command|Purpose|
|-|-|
|`CreateDevDatabaseByMigration`|Create an empty dev DB from EF migrations|
|`CorrectNewDatabase`|Post-process a fresh DB (fix unwanted constructs)|
|`RecreateDevDatabase`|Drop + Create + Correct in one step|
|`DropDevDatabase`|Drop the dev DB|
|`AnaliseDevDatabase`|Analyze schema of the dev DB|
|`AnaliseProdCopyDatabase`|Analyze schema of the prod copy DB|

\---

## If you need a clean dev database from migrations

Use when starting from scratch, or after migrations have changed and the
existing dev DB is unusable.

**Steps**

1. Open the project's menu
2. `Project Tools List` → `CreateDevDatabaseByMigration`

**What it does**

* Creates an empty database (does not drop an existing one)
* Applies all EF Core migrations
* Stops before applying `CorrectNewDatabase`

**Expected output**

* A new database on the configured server
* Schema matches the latest migration
* All tables empty

**Common pitfalls**

* **DB already exists.** The command fails. Run `DropDevDatabase` first,
or use `RecreateDevDatabase`.
* **Migrations don't compile.** EF tools surface the error in the console.
Build the project first.
* **Connection string points to prod.** Double-check the project's
parameters before running. The tool relies on the project's declared
dev connection — it does not validate that the target is non-prod.

\---

## If you need to reset a dev database

Use when the dev DB is in a bad state and you want a single command that
drops, recreates, and corrects it.

**Steps**

1. `Project Tools List` → `RecreateDevDatabase`

**What it does** (in order)

1. Drops the existing dev DB
2. Creates an empty DB by migration
3. Applies `CorrectNewDatabase`

**Expected output**

* Same as `CreateDevDatabaseByMigration`, guaranteed clean

**Common pitfalls**

* **Open connections.** If any tool (Visual Studio, SSMS, EF) holds an
active session, the drop step fails. Close all sessions first.
* **Irreversible.** Local data is lost. There is no recovery once the
drop succeeds.

\---

## If you need to analyze a database schema

Use when investigating drift between dev and prod.

**Steps**

* Dev: `Project Tools List` → `AnaliseDevDatabase`
* Prod copy: `Project Tools List` → `AnaliseProdCopyDatabase`

**What it does**

* Reads metadata from the DB (tables, columns, types, keys)
* Outputs an analysis report (location from project parameters)

**Expected output**

* A report file describing the schema as the tool perceives it

**Common pitfalls**

* **Connection-only.** These commands read directly from the live DB.
They do not depend on migrations. An empty report means the connection
failed — check the connection string, not the project state.

\---

## Related

* [Configuration](../configuration.md) — where DB connection strings live
* [Architecture](../architecture.md) — `LibDatabaseWork`
* [Project Creation](project-creation.md) — initial DB setup for new projects

