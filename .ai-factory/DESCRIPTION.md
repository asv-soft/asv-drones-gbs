# ASV Drones GBS

## Purpose

ASV Drones GBS provides a ground base station service with Real-Time Kinematic
(RTK) support and an ASV Drones desktop plugin for operating the station. The
service connects a u-blox GNSS receiver to MAVLink clients, controls GBS modes,
publishes station telemetry, and forwards RTCMv3 corrections. The plugin adds
GBS controls, telemetry, map integration, and saved fixed-base coordinates to an
ASV Drones host application.

## Technology

- .NET 10 (`net10.0`) for every project.
- .NET Generic Host for the headless service.
- Avalonia 12 and the ASV Avalonia/ASV Drones extension framework for the UI.
- ASV MAVLink, GNSS, IO, configuration, HAL, and common libraries.
- R3 and ObservableCollections for reactive UI state.
- ZLogger for console and rolling-file service logs.
- JSON parameter metadata and T4.Build for generated MAVLink board parameters.

Versions are centralized in `src/Directory.Build.props`. `ProductVersion` is
also the expected version component of release tags.

## Solution

The solution is `src/Asv.Drones.Gbs.sln` and contains four product projects
plus one test project:

| Project | Role | Entry point or public surface |
| --- | --- | --- |
| `Asv.Drones.Gbs` | Headless Linux-oriented GBS runtime service | `src/Asv.Drones.Gbs/Program.cs` |
| `Asv.Drones.Gbs.Contracts` | Shared board-parameter definitions and generated parameter source | `JsonParams/IMavParamsSource.cs`, `JsonParams/MavParams.cs` |
| `Asv.Drones.Plugin.Gbs` | Dynamically loadable ASV Drones UI plugin | `PluginEntryPoint.cs` |
| `Asv.Drones.Plugin.Gbs.App` | Standalone developer host for the plugin | `Program.cs` |
| `Asv.Drones.Gbs.Tests` | Unit and documentation consistency tests | xUnit test classes |

The runtime service and plugin both reference the contracts project. The
standalone app references the plugin. The test project references the service,
contracts, and plugin. The plugin and standalone app consume ASV Drones through
NuGet packages by default or sibling project references when
`UseLocalAsvDrones=true`.

## Runtime service

`Asv.Drones.Gbs` builds a Generic Host and registers, in order:

1. system time, exception handling, logging, and `usersettings.json` storage;
2. the MAVLink server and GBS work-mode handler;
3. welcome/status reporting;
4. operating-system control;
5. GPIO and RGB LED services;
6. u-blox connection discovery and the RTK handler.

The host is built synchronously, started, and held by `WaitForShutdown()`.
`DOTNET_ENVIRONMENT` selects the environment. When it is absent, Debug builds
use `Virtual` and non-Debug builds use `Production`.

The MAVLink server exposes heartbeat, status-text, parameter, diagnostic, and
ASV GBS microservices. Runtime identity comes from the generated `BRD_SYS_ID`
and `BRD_COM_ID` parameters. Production configuration listens on
`tcps://0.0.0.0:7341?reconnect=0`, opens `/dev/ttyS1` for another MAVLink link,
and uses `/dev/ttyS2` for the GNSS receiver.

The RTK handler prepares a u-blox connection, reads GNSS/RTCM/UBX traffic,
supports idle, survey-in, and fixed-base operation, publishes GBS telemetry,
and tunnels selected RTCMv3 messages over MAVLink. Board configuration changes
are persisted through the JSON configuration service.

## Desktop plugin

`Asv.Drones.Plugin.Gbs` extends an existing ASV Drones shell. Its default
registration graph is:

`GbsPlugin -> Core -> Services` and
`GbsPlugin -> Shell -> Pages -> Settings + FlightMode`.

The plugin contributes:

- a GBS flight-mode anchor and widget;
- actions for auto/survey-in, fixed, idle, cancel, base-station location, and
  host telemetry configuration;
- telemetry for mode, accuracy, DGPS rate, observation duration, visible
  satellites, and constellation-specific satellite counts;
- a settings page for adding and deleting named fixed-base coordinates;
- English and Russian resources.

It does not create a second desktop shell. `Asv.Drones.Plugin.Gbs.App` is a
separate developer executable that assembles the normal ASV Drones modules,
plugin discovery, logging, user settings, single-instance behavior, and the GBS
plugin into an Avalonia desktop process.

## Contracts and generated parameters

`src/Asv.Drones.Gbs.Contracts/JsonParams/board.params.json` owns the parameter
metadata. `MavParams.tt` transforms that JSON into `MavParams.cs` during the
contracts build. The generated C# file must not be edited manually; changes
belong in the JSON source or T4 template. Static helpers live in
`MavParams.Static.cs`, and consumers depend on `IMavParamsSource`.

The generated set currently contains seven board parameters for MAVLink system
and component identity, serial number, reboot, shutdown, service restart, and
MAVLink v2 wrapping.

## Build, test, and delivery

Development commands are run from the repository root and target
`src/Asv.Drones.Gbs.sln`. GitHub tag workflows package the contracts and plugin
projects. Development tags publish to GitHub Packages; stable and release
candidate tags publish to both GitHub Packages and NuGet.org.

The repository includes a baseline automated test suite and pull-request CI for
formatting, restore, Release build, documentation consistency, and tests. UI
integration coverage remains deferred until suitable ASV Drones/Avalonia host
test infrastructure is available.

## Documentation map

`README.md` is the landing page. Detailed development, architecture,
configuration, deployment, plugin, and release guidance lives under `docs/`.
Known limitations are documented explicitly and distinguish observations in
this repository from deployment assumptions that require external confirmation.
