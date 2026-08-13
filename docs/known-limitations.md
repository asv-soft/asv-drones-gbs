# Known limitations and external unknowns

This page separates behavior confirmed in the repository from controls that may
exist only in deployment infrastructure or upstream dependencies. Absence of a
control in this repository is not proof that a deployed system lacks it.

## Confirmed configuration limitations

| Finding | Evidence and impact |
| --- | --- |
| Nested MAVLink settings do not receive committed base values | `MavlinkServerOptions` binds only `Mavlink`, while `Heartbeat`, `StatusText`, `Diagnostic`, and `Params` are top-level in `appsettings.json`. Library defaults are effective unless another provider supplies nested values. |
| `Mavlink.CfgPrefix` has no matching option property | The value is present in JSON but absent from `MavlinkServerOptions`; parameter prefix comes from `Mavlink.Params.CfgPrefix`. |
| Startup option validation is absent | MAVLink, RTK, and LED options use `Bind(...)` without `Validate*`/`ValidateOnStart()`. Invalid values can fail when services are constructed or first used. |
| Unknown environments can use unsafe defaults | Without an environment override, MAVLink has no connections while RTK defaults to enabled on `/dev/ttyACM0`. |
| Unused settings exist | `Rtk.GbsStatusRateMs` and `MavlinkServerOptions.Charts` have no consumer in current service code. |

## Confirmed security-sensitive surfaces

- Production binds `tcps://0.0.0.0:7341?reconnect=0`.
- The MAVLink persistent parameter server exposes command descriptors that map
  to service restart, OS reboot, and OS shutdown.
- No authentication/authorization policy, certificate provisioning, firewall
  rule, or caller allow-list for these surfaces is implemented in this
  repository.
- Unix reboot and poweroff start `/usr/bin/sudo /bin/systemctl ...`; the outcome
  and authorization depend on deployment sudo policy.

Deployment owners must establish who can reach the listener and mutate command
parameters. Do not infer security from the `tcps` connection-string scheme
without confirming the ASV IO transport configuration and deployed credentials.

## Confirmed portability and lifecycle limitations

- macOS is routed to `SystemControlServiceUnix`, which invokes Linux-specific
  `/bin/systemctl`; reboot and shutdown are therefore not portable as written.
- Software restart calls `Environment.Exit(0)` and does not request graceful
  Generic Host shutdown.
- `IsRebootRequested` is initialized to `false` and is never updated, so the
  reboot-specific LED animation cannot be reached through current code.
- Service settings, logs, and bundled extraction depend on the process working
  directory unless deployment overrides their paths/environment.
- The committed `run.sh` forces `Virtual`; it is not a Production launcher.

## Confirmed observability and reliability limitations

- Logging clears default providers and enforces an Information minimum. The
  Trace levels requested by Development and Virtual configuration may not be
  emitted.
- The console exception formatter writes only the exception message, which can
  hide stack detail even when the logging call includes an exception object.
- `UpdateStatus` and `InitUbxDevice` are `async void` callbacks. Both contain
  broad catch paths; status update exceptions are silently discarded.
- `UBloxDeviceConnectionsService.SetUpConnection()` converts setup exceptions
  to `false` without logging the exception context at that boundary.
- `UbxRtkDevice.Init()` registers router subscriptions on each initialization
  attempt, but a failed attempt does not dispose subscriptions that were already
  registered. A retry after a late initialization failure can therefore add
  duplicate NAV-SVIN, NAV-PVT, and RTCMv3 handlers until the device is disposed.
- The survey-in status branch dereferences `_svIn.Value.Active` and
  `_svIn.Value.Location` without a null guard. A status update before the first
  NAV-SVIN message can throw; the broad `UpdateStatus` catch then suppresses the
  failure and skips the remainder of that update.
- The `UBloxRtkHandler` constructor registers device-collection/GBS callbacks
  and starts the periodic status timer before `StartAsync()` checks
  `Rtk.IsEnabled`. Setting `Rtk.IsEnabled=false` skips connection startup but
  does not prevent those callbacks and timer from being created.
- Protocol builders receive `IMeterFactory`, but this repository configures no
  metrics exporter and has no demonstrated consumer of `GbsMetrics`.
- RTCM forwarding intentionally drops concurrent packets and records only one
  in-flight send. Slow MAVLink transport can therefore reduce correction data.
- Some operational failures are sent through MAVLink status text; they are not
  guaranteed to appear in local logs when no client is connected.

## Testing boundary

The baseline test project covers deterministic metadata, configuration, and
pure helper behavior. Avalonia views, host extension integration, live MAVLink,
u-blox hardware, serial baud discovery, GPIO, OS control, and end-to-end RTCM
delivery are not integration-tested in this repository. UI integration remains
deferred until suitable ASV Drones/Avalonia host test infrastructure is
available.

## External questions requiring deployment confirmation

- What network or upstream component authenticates and authorizes MAVLink
  clients?
- How are transport certificates/keys provisioned and rotated, if applicable?
- Which firewall rules restrict the wildcard listener?
- Which Linux account runs the service, and what exact sudo capabilities does
  it receive?
- How are serial/GPIO devices named, permissioned, and kept stable across
  reboots?
- Which process supervisor interprets exit code 0 as a restart request?
- Which ASV Drones host/API versions are deployed with the plugin?
- Where are logs and metrics collected, retained, and alerted on?
- What hardware or staging procedure validates RTK modes and RTCMv3 quality
  before production rollout?

Until answered by deployment evidence, these remain unknowns rather than
features or defects confirmed by this codebase.
