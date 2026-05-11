# Related Projects

SupportTools coordinates work across local and remote machines using
two companion projects that live in their own repositories.

## WebAgent

Repository: [https://github.com/merabza/WebAgent](https://github.com/merabza/WebAgent)

A long-running HTTP service installed on each managed server. It
exposes the same capabilities SupportTools has locally — install a
program, manage a service, run a database operation — but reachable
over the network through an API.

SupportTools talks to WebAgent via the contracts in the
`WebAgentContracts` sibling repo (`WebAgentDatabasesApiContracts`,
`WebAgentProjectsApiContracts`).

A remote deployment in SupportTools is, in practice, a sequence of
HTTP calls from your developer machine to WebAgent on the target
server, with WebAgent doing the actual work in the server's local
filesystem and service manager.

**What WebAgent does that SupportTools can't reach directly:**

* Install/uninstall programs on the server
* Start/stop services
* Read service version (`VersionChecker` calls WebAgent's version
endpoint)
* Apply encoded `appsettings.json` to a service's install location
* Run database operations against databases the developer can't reach

\---

## WebAgentInstaller

Repository: [https://github.com/merabza/WebAgentInstaller](https://github.com/merabza/WebAgentInstaller)

A small bootstrapper used to install and update WebAgent itself.

You install WebAgentInstaller once on a new server. From there,
WebAgentInstaller can pull and install new versions of WebAgent. This
is the chicken-and-egg solution: WebAgent can't update itself while
running, so WebAgentInstaller does it.

After WebAgentInstaller is on a server, you never touch it manually —
SupportTools drives it through the same contracts as WebAgent.

\---

## When to use which

|Scenario|Tool that does the work|
|-|-|
|Operations on the developer's own machine|SupportTools (local path)|
|Operations on a remote server|SupportTools → WebAgent (remote path)|
|First-time setup of a new server|Manual install of WebAgentInstaller, then WebAgent|
|Updating WebAgent on a server|SupportTools → WebAgentInstaller → installs new WebAgent|

The `IsLocal` flag on a `ServerDataModel` picks the path. See
[Configuration](configuration.md).

\---

## Version coupling

The three tools share contracts via the `WebAgentContracts` repo. A
version mismatch between the SupportTools side and the WebAgent side
shows up as silent failures during health checks — see
[Deployment](use-cases/deployment.md) for the "WebAgent version
mismatch" pitfall.

The safest practice: update WebAgent on a server **before** you deploy
a project whose SupportTools tooling has been upgraded.

\---

## Related

* [Deployment](use-cases/deployment.md) — how the remote path is used
* [Architecture](architecture.md) — `LibSupportToolsServerWork` is the
client side

