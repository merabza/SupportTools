# Deployment

Application install, update, and service management — on a local machine
or on a remote server via WebAgent.

The remote path uses [WebAgent](../related-projects.md) as the agent on
the target server. The local path writes directly to a configured install
folder.

These commands operate on a project after a server has been configured
through `Project → Server Infos List → Create New Server Info`. See
[Configuration](../configuration.md) for the field reference.

## Quick Reference

|Command|Purpose|
|-|-|
|`ProgPublisher`|Build a deployable package and upload it to the server|
|`ProgramInstaller`|Install (or replace) the program package on the server|
|`ProgramUpdater`|Combined build + install|
|`AppSettingsEncoder`|Encrypt `appsettings.json` for deployment|
|`AppSettingsInstaller`|Push an encrypted `appsettings.json` to the server|
|`AppSettingsUpdater`|Encode and push in one step|
|`ServiceStarter`|Start the installed service on the server|
|`ServiceStopper`|Stop the running service|
|`VersionChecker`|Query the running service for its version|
|`ProgRemover`|Remove the program and its service from the server|
|`ServiceInstallScriptCreator` / `ServiceRemoveScriptCreator`|Generate install/remove scripts only|

\---

## Local vs. remote

Each `ServerInfo` references a `ServerData` entry, and the `IsLocal` flag
on that entry decides the transport:

* **Local (`IsLocal = true`)** — files are written to
`LocalInstallerSettings.InstallFolder`, scripts are executed on the
current machine. Requires sufficient OS permissions (service install
typically needs elevation).
* **Remote (`IsLocal = false`)** — files are sent to `WebAgentName` over
HTTP; the WebAgent on the server performs the install. Requires
WebAgent to be running and reachable at the configured URL with a
valid API key.

\---

## If you need to deploy a fresh build to a server

The common path: encode settings, publish a package, install it, start
the service.

**Steps**

1. `Project Tools List` → `AppSettingsEncoder` — produces an encrypted
`appsettings.json`
2. `Project Tools List` → `ProgPublisher` — builds the project and
uploads the artifact
3. `Project Tools List` → `ProgramInstaller` — installs the artifact on
the target server
4. `Project Tools List` → `AppSettingsInstaller` — pushes the encrypted
settings to the running service location
5. `Project Tools List` → `ServiceStarter` — starts the service
6. `Project Tools List` → `VersionChecker` — confirms the new version is
serving traffic

Or use the combined commands: `ProgramUpdater` (publish + install) and
`AppSettingsUpdater` (encode + install).

**Expected output**

* The new build is on the server
* The service is running with the new encrypted settings
* `VersionChecker` reports a version matching the build

**Common pitfalls**

* **WebAgent version mismatch.** If the WebAgent on the server is
older than the SupportTools deployment contracts, `VersionChecker`
retries 10 times (3 s each ≈ 30 s) before failing silently. Update
WebAgent on the server before deploying.
* **AppSettings key mismatch.** The encryption key is derived from
`KeyGuidPart` (per-project) and the capitalized `ServerName`. If
either differs between the encoding machine and the server, decryption
fails at startup and the service refuses to start with cryptic errors.
* **Missing exchange settings.** `FileStorageNameForExchange` and
`SmartSchemaNameForExchange` are required globally in
`SupportToolsParameters`. Encoding fails fast if either is unset, even
when the project's own settings are complete.
* **Service account permissions.** `ServiceUserName` on the server
must have rights to create directories, install services, and write
to the registry. The deploy script reports success up to the point of
service registration, then the service fails to start.
* **Port and `ApiVersionId` interaction.** Health checks require
`ServerSidePort > 0` and a non-empty `ApiVersionId`. If only one is
set, the operation fails. If port is `0`, health checks are skipped
silently — useful, but be aware.

\---

## If you need to stop, start, or check a service

Use during routine maintenance, or to verify the running version.

**Steps**

* Stop: `Project Tools List` → `ServiceStopper`
* Start: `Project Tools List` → `ServiceStarter`
* Version: `Project Tools List` → `VersionChecker`

**Expected output**

* Service state transitions reported in the console
* `VersionChecker` calls the service's version endpoint and prints the
result

**Common pitfalls**

* **Stop on a non-existent service.** The command returns success-like
output even when the service is not installed. Cross-check with
`VersionChecker` if unsure.
* **`VersionChecker` requires the right endpoint.** It expects either
`TestTools.Controllers.TestController` or the proxy version endpoint.
Custom builds without these endpoints always report version-fetch
failure.

\---

## If you need to remove an installed program

Use when retiring a service, or before a clean reinstall.

**Steps**

1. `Project Tools List` → `ServiceStopper` (recommended first)
2. `Project Tools List` → `ProgRemover`

**What it does**

* Stops the service if it's still running
* Removes the installed files
* Unregisters the service

**Common pitfalls**

* **Locked files.** If any process still holds files in the install
folder, removal partially fails. Stop the service first.
* **Leftover scheduled tasks.** The tool removes the service but not
unrelated scheduled tasks pointing at the same binaries. Clean those
manually.

\---

## How AppSettings encoding works

Useful background when debugging deploy failures.

1. Reads a key list from `project.AppSetEnKeysJsonFileName` — only the
configured JSON paths are encrypted, the rest is copied as-is.
2. Builds the symmetric key as
`SHA256(KeyGuidPart + ServerName.Capitalize())`.
3. Merges the source `appsettings.json` with user secrets if a `.csproj`
is present.
4. Encrypts the targeted string values with AES-256-CBC and a random IV
prepended to each value before Base64 encoding.
5. Stamps `VersionInfo.AppSettingsVersion` with a timestamp so the
server can detect drift.
6. Writes the result to `AppSettingsEncodedJsonFileName`.

If decryption ever silently fails on the server, the version stamp is
the first thing to check — a mismatch means the encrypted file the
service has on disk was produced with a different key.

\---

## Related

* [Configuration](../configuration.md) — server info, key settings
* [Architecture](../architecture.md) — `LibAppInstallWork`,
`LibSupportToolsServerWork`
* [Related Projects](../related-projects.md) — WebAgent, WebAgentInstaller

