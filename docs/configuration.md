# Configuration

## Environment selection and precedence

`Program.cs` reads `DOTNET_ENVIRONMENT`. When it is unset, Debug builds select
`Virtual` and non-Debug builds select `Production`. The .NET Generic Host loads
base configuration, the matching environment file, environment variables, and
command-line arguments with later providers overriding earlier values.

The committed service files are:

| File | Role |
| --- | --- |
| `src/Asv.Drones.Gbs/appsettings.json` | Base MAVLink microservice settings |
| `appsettings.Development.json` | Local serial/TCP development and RTK defaults |
| `appsettings.Virtual.json` | Loopback MAVLink with RTK and LED disabled |
| `appsettings.Production.json` | Wildcard/serial MAVLink and `/dev/ttyS2` GNSS |

An unknown environment with no matching override keeps base/default option
values: no configured MAVLink connections and RTK enabled with its class default
`serial:/dev/ttyACM0?br=115200`.

## MAVLink

`MavlinkServiceMixin` binds `MavlinkServerOptions` only from the `Mavlink`
section. Its properties are `Connections`, `Heartbeat`, `StatusText`, `Params`,
`Diagnostic`, `Charts`, and `Gbs`.

The base file currently places `Heartbeat`, `StatusText`, `Diagnostic`, and
`Params` at the top level, so those values do not bind to the nested option
properties. `Mavlink.CfgPrefix` is present in committed files but
`MavlinkServerOptions` has no `CfgPrefix` property. This mismatch is retained as
a known configuration limitation and covered by an effective-binding test.

Committed endpoints:

| Environment | MAVLink connections |
| --- | --- |
| Development | `tcps://127.0.0.1:7341`, `tcp://127.0.0.1:5762` |
| Virtual | `tcps://127.0.0.1:7341` |
| Production | `tcps://0.0.0.0:7341?reconnect=0`, `serial:/dev/ttyS1?br=115200` |

## RTK and GNSS

`RtkDeviceOptions` binds from `Rtk`.

| Setting | Meaning |
| --- | --- |
| `IsEnabled` | Start device connection and RTK handler work |
| `IsEnabledRtk` | Register auto/fixed commands and forward RTCMv3 |
| `IsAutoStartFixedMode` | Program fixed mode after successful device initialization |
| `FixedModeLat/Lon/Alt/Accuracy` | Coordinates used by auto-start fixed mode |
| `ConnectionString` | GNSS receiver port |
| `UpdateStatusFromDeviceRateMs` | Device polling period |
| `RtcmV3MessagesIdsToSend` | Allowed RTCMv3 IDs forwarded over MAVLink |
| `MessageRateHz` | Requested u-blox NAV message rate |
| `ReconnectTimeoutMs` | Delay before reconnect/init retry |
| `GbsStatusRateMs` | Configured but not read by current service code |

Development uses `serial:COM4?br=115200`; Virtual uses a disabled
`serial:COM8?br=115200` definition; Production uses
`serial:/dev/ttyS2?br=115200`.

## LED

`LedServiceOptions` binds from `Led` and contains `IsEnabled`, animation tick
duration, and RGB GPIO pin/chip/inversion values. All committed service
environments set `Led.IsEnabled=false`, so the null LED implementation is used
unless deployment configuration overrides it.

## Mutable board parameters

The headless service registers a separate `Asv.Cfg.JsonOneFileConfiguration`
for relative `usersettings.json`, with creation enabled and a 500 ms reload
interval. The MAVLink parameter server stores board identity, system-control
commands, serial metadata, and MAVLink v2 wrapping values there under the
parameter-server prefix. This store is distinct from Generic Host
`appsettings*.json` configuration.

See [Board parameters](parameters.md) for all descriptors and consumers.

## Logging

`AddDefaultLogging()` clears default providers, sets an explicit Information
minimum, and adds ZLogger console and rolling-file outputs. Files are written to
`logs/yyyy-MM-dd_<index>.logs` and roll at approximately 1 MiB. Console output
includes time, short level, category, scopes, and exception message.

Development and Virtual files request `Logging.LogLevel.Default=Trace`, but the
explicit `SetMinimumLevel(LogLevel.Information)` may prevent Trace events from
appearing. No metrics exporter is configured even though protocol components
receive `IMeterFactory`.

## Standalone plugin app settings

`src/Asv.Drones.Plugin.Gbs.App/appsettings.json` configures the developer UI
host, not the headless service. It defines desktop logging, unhandled-exception
behavior, plugin discovery, `user_settings.json` autosave, single-instance
mutex/argument forwarding, and log-viewer storage. Its Development and
Production override files are currently empty objects.

## Validation status

No startup `ValidateOnStart()` rules are registered for MAVLink, RTK, or LED
options. Invalid connection strings, device values, or nested binding errors may
therefore surface only when the corresponding singleton or hosted service is
constructed.
