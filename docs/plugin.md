# GBS desktop plugin

## Registration graph

The package entry point is
`src/Asv.Drones.Plugin.Gbs/PluginEntryPoint.cs`. An ASV plugin loader calls
`IPluginAppBuilder.Register()`, which delegates to `RegisterGbsPlugin()`.

Without a custom callback, every registration builder calls its
`RegisterDefault()` method. The resulting graph is:

```text
RegisterGbsPlugin
├── RegisterCore
│   └── RegisterServices
│       └── RegisterGbsDeviceManagerExtension
└── RegisterShell
    └── RegisterPages
        ├── RegisterGbsSettings
        └── RegisterFlightMode
            ├── RegisterAnchors
            └── RegisterWidgets
                └── RegisterGbsWidget
                    ├── RegisterActions
                    │   ├── RegisterGbsWidgetActions
                    │   ├── RegisterGbsAnchorActions
                    │   └── RegisterDialogs
                    └── RegisterSections
                        ├── RegisterTelemetrySection
                        │   └── RegisterTelemetryItemFactories
                        └── RegisterSatelliteCountSection
```

The callback-based builders allow a host to replace any default subtree with a
custom registration selection. The default plugin registers all branches.

## Host extension points

The plugin composes with services owned by the ASV Drones host:

| Extension point | GBS contribution |
| --- | --- |
| `IDeviceManagerExtension` | Recognizes and extends GBS MAVLink client devices |
| `ITreePageMenuItem` | Adds the GBS saved-coordinates settings group |
| Flight-mode anchor extensions | Adds a GBS map anchor for a compatible device |
| `IClientDeviceWidgetCreationHandler` | Creates the `gbs` flight widget |
| Widget and anchor action extensions | Adds GBS commands and map/telemetry actions |
| `IExtensionFor<IGbsFlightWidget>` | Adds telemetry and satellite-count sections |
| `ITelemetryItemFactory` | Supplies accuracy, mode, DGPS rate, observation, and satellite tiles |
| View locator / view-model registry | Maps GBS interfaces and view models to Avalonia views |

The plugin does not own the root service provider, main window, navigation
service, plugin loader, vehicle/device discovery, map, or application lifetime.
Those capabilities must already be registered by the host.

## Compatibility gate

A GBS widget or action is created only for a `GbsClientDevice` exposing an
`IAsvGbsExClient` microservice. Missing GBS capability results in no extension,
not a partially functional control.

## Plugin package versus standalone developer app

`Asv.Drones.Plugin.Gbs` is a library loaded into an existing ASV Drones
application. Its only process-level entry point is the plugin registration
adapter. It assumes that the host has already configured Avalonia, the desktop
shell, device APIs, plugin infrastructure, navigation, logging, localization,
maps, charts, IO, user configuration, and application lifetime.

`Asv.Drones.Plugin.Gbs.App` is a `WinExe` used to run and debug the plugin as a
complete desktop application. Its `Program.BuildAvaloniaApp()` method creates
the missing host around the library:

- selects Windows, X11, or Avalonia Native platform options;
- enables developer tools in Debug builds;
- registers core controls and services, desktop shell, plugin bootloader and
  manager, map, charts, IO, optional launcher, and the ASV Drones application;
- configures logging, crash handling, localization, theme, dialogs, navigation,
  file association, user settings, and single-instance argument forwarding;
- directly calls `RegisterGbsPlugin()` so the in-repository plugin is always
  available during development.

Standalone app settings live under `src/Asv.Drones.Plugin.Gbs.App/` and are not
the headless GBS service settings. They control the desktop host (for example,
plugin discovery, `user_settings.json`, logs, and the single-instance mutex).
Deploying only the plugin package does not deploy or start the standalone app or
the RTK service.

## GBS mode transitions

The service derives its reported custom mode from the u-blox `CFG-TMODE3`
state. `Loading` and `Error` are initialization states; operational transitions
are:

```text
                    survey active
Idle ── StartAuto ───────────────> AutoInProgress
  ^                                      |
  |                                      | survey completes
  | StartIdle / Cancel                   v
  +------------------------------------ Auto
  |
  |             TMODE3 fixed applied
  +── StartFixed ─────────────────────> Fixed
```

- **Idle** disables u-blox time mode, forces RTCM output off, clears DGPS rate,
  and reboots the receiver after the configuration change.
- **Auto / survey-in** programs duration and accuracy criteria. While survey-in
  is active, the mode is `AutoInProgress`, current navigation position is
  reported, and RTCM output remains off. When survey-in completes, the mode is
  `Auto`, the surveyed location is reported, and RTCM output starts.
- **Fixed** validates latitude, longitude, altitude, and accuracy, programs the
  receiver, waits up to ten 500 ms polls for fixed `TMODE3`, and then starts
  RTCM output. Latitude must be in `[-90, 90]`, longitude in `[-180, 180]`, the
  latitude/longitude pair cannot both be zero, all numeric inputs must be
  finite, and accuracy must be between 0.001 m and 100 m.

Auto and fixed commands are not registered when `Rtk.IsEnabledRtk=false`; idle
remains available. The handler rejects overlapping commands and commands issued
before device initialization.

The plugin exposes Auto and Fixed only while the reported mode is `Idle`. Idle
is available from completed `Auto` or `Fixed`. Cancel is available while
`AutoInProgress` or `FixedInProgress` and sends the same idle command. The
service currently reports a fixed state only after the receiver confirms it;
`FixedInProgress` may still be reported by the protocol/client during command
execution.

## Saved coordinates and user configuration

The plugin uses the host-provided `Asv.Cfg.IConfiguration`; the library does not
choose the backing file. The standalone developer app configures that host
service with `UserConfiguration.FilePath=user_settings.json` and a 500 ms
autosave interval.

`FixedModeSavedCoords` stores a list of `FixedModeConfig` records. Each record
contains:

- required display `Name`;
- `Latitude`, `Longitude`, and `Altitude` in SI/geographic values;
- fixed-position `Accuracy` in meters.

The GBS settings page loads the list into an observable collection. Adding a
record validates the name, coordinates, and accuracy before calling
`configuration.Set(...)`. Removing a selected record requires confirmation and
then writes the whole updated list. The fixed-mode dialog reads the same list,
can copy a saved record into its fields, and can append a newly named record.
The action persists the resulting list when the dialog closes, including saved
records created before a command is cancelled.

`AutoModeConfig` separately remembers the last survey accuracy and observation
duration. Those values are persisted after the Auto dialog is accepted and are
used as defaults the next time it opens (with minimums of 0.1 m and 1 s).

## Telemetry and map behavior

The service publishes GBS state through `IAsvGbsServerEx`; the plugin consumes
the matching client microservice. The flight widget includes these inline
telemetry items:

| Item | Source | Presentation |
| --- | --- | --- |
| Mode | `CustomMode` | Protocol enum name without the common prefix |
| Accuracy | `AccuracyMeter` | Host-selected distance unit, two decimals |
| DGPS rate | `DgpsRate` | Scaled bytes per second |
| Observation | `ObservationSec` | Host-selected relative time unit |
| Visible satellites | `AllSatellites` | Count |

The satellite section visualizes separate counts for GPS, SBAS, Galileo,
BeiDou, IMES, QZSS, and GLONASS. Reactive streams suppress duplicate values,
throttle high-frequency telemetry tiles to 200 ms, and deliver UI mutations on
the Avalonia UI thread.

Position telemetry is used by the GBS map anchor and the Locate Base Station
action. The action centers the flight-mode map on the current GBS position. A
host telemetry-configuration action is also attached to the widget so users can
select or arrange the registered telemetry items.
