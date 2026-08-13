# System architecture

## Components

| Component | Responsibility |
| --- | --- |
| GBS service | Owns MAVLink server, parameter persistence, u-blox integration, RTK mode control, RTCM forwarding, system control, and LED state |
| Contracts | Owns public parameter descriptors generated from JSON |
| GBS plugin | Extends an ASV Drones host with device recognition, actions, settings, map anchors, and telemetry |
| Standalone plugin app | Supplies a complete Avalonia/ASV Drones host for plugin development |
| Tests | Verify deterministic metadata, configuration, helpers, and documentation consistency |

The service and plugin share contracts but do not depend on each other. The
standalone app is the UI composition root and depends on the plugin. Tests
reference the service, contracts, and plugin but are not a production
dependency.

## Runtime data flow

```text
u-blox serial device
  -> ASV IO router (NMEA / RTCMv3 / UBX)
  -> UBloxRtkHandler
     -> GBS state and telemetry -> MAVLink GBS microservice -> desktop client
     -> serialized RTCMv3 ------> MAVLink RTCM tunnel ------> rover/client

MAVLink parameter client
  <-> ParamsServerEx
  <-> usersettings.json
  -> system-control command handlers
```

The runtime creates the MAVLink router first and the u-blox router later when
the hosted RTK handler is constructed. Device initialization configures message
rates, disables NMEA, subscribes to selected UBX/RTCM messages, and sets GBS
state. A failed initialization schedules a retry after `Rtk.ReconnectTimeoutMs`.

## Startup and lifetime

`Program.cs` selects an environment, builds a .NET Generic Host, calls
`Start()`, and blocks in `WaitForShutdown()`. Hosted services start in
registration order: exception hooks, work mode, welcome/status publication,
system control, LED, then RTK.

Singletons own routers, configuration, hardware services, and time. Hosted
services own subscriptions and mode orchestration. The MAVLink and IO disposable
builders aggregate protocol resources for shutdown.

## Plugin flow

An ASV Drones plugin loader calls `PluginEntryPoint.Register()`. Registration
adds device-manager, settings, flight-mode, action, view, and telemetry
extensions to host registries. When a discovered `GbsClientDevice` exposes
`IAsvGbsExClient`, the host creates a GBS widget and applies registered
extensions. R3 streams carry device state to view models, and UI mutations are
scheduled on the Avalonia dispatcher.

See [GBS desktop plugin](plugin.md) for the full registration graph and user
behavior.

## Configuration and generated metadata

Generic Host configuration binds immutable startup options from JSON,
environment variables, and command-line providers. Mutable board parameters
are a separate `Asv.Cfg` store backed by `usersettings.json`. JSON parameter
metadata is transformed to C# by T4 and supplied to the MAVLink parameter
server.

See [Configuration](configuration.md), [Board parameters](parameters.md), and
the canonical project-level
[ARCHITECTURE.md](../.ai-factory/ARCHITECTURE.md) for DI registrations,
dependency constraints, and external boundaries.

## External boundaries

The repository integrates with a u-blox receiver, MAVLink peers, an ASV Drones
host, optional GPIO, OS control commands, and package registries. It does not
contain network authentication, firewall rules, a Linux service unit, serial or
GPIO permission setup, or credentials. Those are deployment responsibilities.
