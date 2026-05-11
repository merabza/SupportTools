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
|`PrepareProdCopyDatabase`|Prepare a prod copy for scaffold processing|
|`ScaffoldSeederCreator`|Generate scaffold + seed code/JSON from prod copy|
|`JsonFromProjectDbProjectGetter`|Extract DB rows into JSON files|
|`SeedData`|Load JSON files into the dev DB|

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

## If you need to copy production data into dev

A multi-step pipeline. Intermediate JSON files are the contract between
extraction and loading.

**Steps**

1. Obtain a prod copy (out of scope of this tool — use your DBA's process
or a backup restore)
2. `Project Tools List` → `PrepareProdCopyDatabase`
— cleans up the prod copy so it can be scaffolded
3. `Project Tools List` → `JsonFromProjectDbProjectGetter`
— reads rows from each table, writes one JSON file per table
4. `Project Tools List` → `SeedData`
— reads the JSON files, inserts rows into the dev DB

**Expected output**

* A folder of `<TableName>.json` files (location from project parameters)
* Dev DB populated with the same data as the prod copy

**Common pitfalls**

* **Order dependency.** `SeedData` relies on the dependency graph
produced by `ScaffoldSeederCreator`. Run the scaffold step at least
once for this project before seeding.
* **Identity columns.** If prod rows carry explicit IDs, the dev schema
must permit `IDENTITY\\\_INSERT`. The seeder toggles it per table, but
the schema shape must match.
* **Plaintext on disk.** JSON files contain raw row data, including any
sensitive fields. Keep the output folder outside the repo and out of
git.
* **Large tables.** The extractor loads tables fully into memory. For
tables larger than a few hundred MB, filter at the SQL level before
running.

\---

## If you need to analyze a database schema

Use when investigating drift between dev and prod, or auditing what the
scaffold tool sees.

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
They do not depend on migrations or scaffold output. An empty report
means the connection failed — check the connection string, not the
project state.

\---

## If you need to generate scaffold + seed code from prod

Use when bootstrapping a new project, or refreshing the scaffold model
after a schema change in prod.

**Steps**

1. `Project Tools List` → `ScaffoldSeederCreator`

**What it does**

* Scaffolds against the prod copy
* Generates EF model classes
* Generates seed JSON files
* Generates the seeder code that consumes those JSON files

**Expected output**

* A scaffold project at the configured path
* JSON files alongside it, ready for `SeedData`

**Common pitfalls**

* **Idempotency.** Re-running overwrites generated code. Manual edits in
the scaffold project are lost. Keep customizations in partial-class
files outside the generated path.
* **Reserved-word column names.** If prod columns share names with C#
keywords, generated code may not compile. Adjust the column or use
EF customization hooks.
* **Run order.** `SeedData` will fail if `ScaffoldSeederCreator` has not
been run for this project at least once — the dependency graph the
seeder needs comes from the scaffold step.

\---

## Related

* [Configuration](../configuration.md) — where DB connection strings live
* [Architecture](../architecture.md) — `LibDatabaseWork`, `LibScaffoldSeeder`
* [Project Creation](project-creation.md) — initial DB setup for new projects

