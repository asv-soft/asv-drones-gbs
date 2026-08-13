# Architecture

## Scope

This document describes the code in this repository. ASV Drones host behavior,
network topology, service management, device permissions, and authentication
outside the repository are external concerns unless explicitly stated.

## Project dependency map

```text
Asv.Drones.Gbs
└── Asv.Drones.Gbs.Contracts

Asv.Drones.Plugin.Gbs.App
├── Asv.Drones.Plugin.Gbs
│   ├── Asv.Drones.Gbs.Contracts
│   └── Asv.Drones.Api (package, or sibling source when UseLocalAsvDrones=true)
├── Asv.Drones
└── ASV Avalonia modules

Asv.Drones.Gbs.Tests
├── Asv.Drones.Gbs
├── Asv.Drones.Gbs.Contracts
└── Asv.Drones.Plugin.Gbs
```

| Project | Architectural responsibility | Must not own |
| --- | --- | --- |
| `Asv.Drones.Gbs.Contracts` | Parameter metadata, generated parameter source, shared contracts | Runtime device orchestration or UI |
| `Asv.Drones.Gbs` | Headless MAVLink/RTK service and hardware integration | Desktop shell UI |
| `Asv.Drones.Plugin.Gbs` | ASV Drones extension registrations, actions, settings, and telemetry views | Process-level desktop host setup |
| `Asv.Drones.Plugin.Gbs.App` | Developer desktop composition root | Reusable GBS domain or plugin behavior |
| `Asv.Drones.Gbs.Tests` | Deterministic unit and documentation consistency checks | Production runtime behavior |

The contracts project is the shared lower-level dependency. The service and
plugin do not reference each other. The standalone app is the top-level UI
composition root. Tests are a top-level verification consumer and are not
referenced by production projects.

## Runtime service startup

The composition root is `src/Asv.Drones.Gbs/Program.cs`.

1. Read `DOTNET_ENVIRONMENT`.
2. If it is unset, select `Virtual` in Debug or `Production` otherwise.
3. Create `HostApplicationBuilder` with command-line arguments.
4. Register infrastructure and hosted services through fluent mixins.
5. Build the host.
6. Call `Start()` and then `WaitForShutdown()`.

`IHostedService` instances start in registration order:

1. `ExceptionHandler`
2. `WorkModeHandler`
3. `PrintWelcomeHandler`
4. `SystemControlHandler`
5. `LedHandler`
6. `UBloxRtkHandler`

The order matters: exception hooks are installed first, MAVLink heartbeat/GBS
services start before handlers publish state, and RTK device work begins last.

## Dependency injection registrations

| Registration | Lifetime | Implementation/source |
| --- | --- | --- |
| `TimeProvider` | Singleton | `TimeProvider.System` |
| `Asv.Cfg.IConfiguration` | Singleton | `JsonOneFileConfiguration("usersettings.json", reload: 500 ms)` |
| `IMavParamsSource` | Singleton instance | `MavParams.Instance` |
| `IMavlinkService` | Singleton | `MavlinkServer` |
| `IOptions<MavlinkServerOptions>` | Options | Bound from `Mavlink` |
| `ISystemControlService` | Singleton instance | Windows or Unix implementation selected by OS |
| `IGpioProvider` | Singleton | `LibGpioProvider` |
| `ILedService` | Singleton | `LedService` |
| `IOptions<LedServiceOptions>` | Options | Bound from `Led` |
| `IDeviceConnectionsService` | Singleton | `UBloxDeviceConnectionsService` |
| `IOptions<RtkDeviceOptions>` | Options | Bound from `Rtk` |

Hosted services are registered by `AddHostedService<T>()` and consume these
singletons/options.

## Configuration flow

```text
appsettings.json
  -> appsettings.{Environment}.json
  -> environment variables / command-line providers supplied by Generic Host
  -> IOptions<MavlinkServerOptions>, IOptions<LedServiceOptions>,
     IOptions<RtkDeviceOptions>

board.params.json -> MavParams.tt -> MavParams.cs
  -> IMavParamsSource -> MAVLink parameter server
  <-> usersettings.json through Asv.Cfg.JsonOneFileConfiguration
```

Environment configuration controls connections and hardware behavior. Mutable
MAVLink board parameters use the separate relative `usersettings.json` file.
Changes received by the persistent parameter server are saved through
`JsonOneFileConfiguration` and reloaded on a 500 ms interval.

Only the `Mavlink` section is bound to `MavlinkServerOptions`. In the current
base `appsettings.json`, `Heartbeat`, `StatusText`, `Diagnostic`, and `Params`
are top-level sections; they therefore do not populate the nested option
properties through this binding. This is a documented known limitation, not an
alternate supported configuration model.

## MAVLink boundary

`MavlinkServer` creates a MAVLink v2 protocol and a router named `GBS`, adds the
configured ports, and optionally registers MAVLink v2 extension wrapping. It
then creates:

- heartbeat and status-text servers;
- command and extended command-long servers;
- persistent parameter servers;
- diagnostics;
- base and extended ASV GBS microservices.

The service identity is read from persisted `BRD_SYS_ID` and `BRD_COM_ID`
values. `WorkModeHandler` identifies the component as `MavTypeAsvGbs` and
starts heartbeat and GBS services.

## GNSS and RTK boundary

The `UBloxRtkHandler` constructor creates an ASV IO router for NMEA, RTCMv3,
and UBX protocols. Later, `StartAsync()` initiates serial startup by scheduling
`TryConnectToDevice()`, which asks `IDeviceConnectionsService` to prepare the
serial connection and adds the port to the existing router after preparation
succeeds. The handler uses `IUbxMicroserviceClient` to read navigation/survey
status and configure time mode and RTCM output.

The state machine exposes three operating families:

- idle: u-blox time mode is disabled and RTCM output is stopped;
- auto/survey-in: survey-in is active until its accuracy/duration criteria are
  met, then RTCM output is enabled;
- fixed: validated coordinates and accuracy are written to the receiver and
  RTCM output is enabled.

Selected RTCMv3 packets are serialized and tunneled through the GBS MAVLink
microservice. Only one send is allowed at a time, sends time out after two
seconds, and stale output is reconfigured after ten seconds.

## System and hardware boundaries

- `SystemControlHandler` maps remote persistent-parameter commands to restart,
  reboot, and shutdown operations.
- Windows uses the `shutdown` executable. Linux and macOS currently share the
  Unix implementation, which invokes `/bin/systemctl` for OS operations.
- `LedService` selects a GPIO-backed RGB LED when enabled and a null LED when
  disabled. All committed environment files disable the LED.
- ZLogger writes structured application events to the console and daily/indexed
  rolling files under `logs/`, with a per-file size of approximately 1 MiB.

## Plugin architecture

The plugin entry point implements `IPluginAppBuilder` and delegates to
`RegisterGbsPlugin()`. Registration builders form this graph:

```text
GbsPlugin
├── Core
│   └── Services
└── Shell
    └── Pages
        ├── Settings
        │   └── GBS saved coordinates
        └── FlightMode
            ├── anchors
            ├── actions and dialogs
            └── widget
                ├── telemetry section
                └── satellite-count section
```

These are host extension points. The plugin registers services and extensions
into an existing shell; it does not own application lifetime or construct a
window. Reactive values use R3/ObservableCollections, and UI-bound updates are
marshaled to the Avalonia UI thread.

`Asv.Drones.Plugin.Gbs.App` is a composition root for development. It creates an
Avalonia process, registers the standard core, desktop shell, plugin manager,
map/chart/IO modules, launcher, ASV Drones application, and finally the GBS
plugin. Production plugin loading is owned by its ASV Drones host.

## Generated-code boundary

`board.params.json` and `MavParams.tt` are source files.
`JsonParams/MavParams.cs` is generated output and must never be edited by hand.
`MavParams.Static.cs` is hand-written and may contain stable categories or
helpers that complement generated output. Consumers depend on the generated
descriptors through `IMavParamsSource` or the static `MavParams` members.

## External integrations

| Integration | Purpose | Repository boundary |
| --- | --- | --- |
| u-blox receiver | Position, survey-in, fixed mode, RTCMv3 | Serial connection configured under `Rtk` |
| MAVLink clients | Commands, parameters, telemetry, RTCM tunnel | Connections configured under `Mavlink` |
| ASV Drones host | Plugin discovery, UI shell, vehicle/device APIs | NuGet packages or sibling source |
| GPIO | Optional RGB status LED | `Asv.Hal`/libgpiod environment |
| OS service manager | Reboot/shutdown | Windows `shutdown` or Unix `/bin/systemctl` |
| GitHub Packages/NuGet.org | Contracts and plugin distribution | Tag-triggered GitHub Actions |

Authentication, firewalling, Linux service unit ownership, serial permissions,
and hardware wiring are deployment responsibilities not implemented in this
repository.
