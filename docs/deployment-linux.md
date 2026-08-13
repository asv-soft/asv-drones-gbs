# Linux deployment

## What the repository supplies

`Asv.Drones.Gbs` is the deployable headless service. Its project copies all
four service `appsettings*.json` files and `run.sh` to publish output. The
repository does not supply an installer, systemd unit, container image,
firewall configuration, device permissions, or production secrets.

## Publish

Choose the runtime identifier for the target hardware (for example,
`linux-x64` or `linux-arm64`) and publish from the repository root:

```bash
dotnet publish src/Asv.Drones.Gbs/Asv.Drones.Gbs.csproj \
  --configuration Release \
  --runtime <linux-rid> \
  --self-contained true \
  -p:PublishSingleFile=true
```

The output is under
`src/Asv.Drones.Gbs/bin/Release/net10.0/<linux-rid>/publish/`. Preserve the JSON
configuration files beside the executable.

## Production launch contract

Run the executable from a stable writable working directory because
`usersettings.json`, `logs/`, and single-file extraction are relative concerns.
Set the environment explicitly:

```bash
cd /opt/asv-drones-gbs
DOTNET_ENVIRONMENT=Production ./Asv.Drones.Gbs
```

The checked-in `run.sh` intentionally forces `DOTNET_ENVIRONMENT=Virtual` and
verifies Virtual configuration files. It is a safe virtual/development launcher
and must not be used as a production launcher without an intentional review and
change.

## Example service-manager inputs

A production service definition should provide at least:

- an unprivileged service account;
- an explicit `WorkingDirectory` containing the executable and settings;
- `DOTNET_ENVIRONMENT=Production`;
- restart policy appropriate for `Environment.Exit(0)` software restart;
- write access to the working directory or dedicated settings/log paths;
- serial access to `/dev/ttyS1` and `/dev/ttyS2` when committed Production
  settings are used;
- GPIO access only when LED support is enabled;
- a deliberate authorization model for reboot and shutdown.

Do not copy a generic systemd unit without resolving the target user, runtime
identifier, installation directory, device ownership, and restart policy for
the actual device.

## Network and hardware

Committed Production settings open `tcps://0.0.0.0:7341?reconnect=0`, add a
MAVLink serial link at `serial:/dev/ttyS1?br=115200`, and use
`serial:/dev/ttyS2?br=115200` for the u-blox receiver. No authentication or authorization layer
for the wildcard listener or remote board-parameter commands was found in this
repository. Bind or firewall the listener according to the deployment threat
model and confirm whether upstream ASV/MAVLink infrastructure supplies access
control.

The Unix system-control implementation starts `/usr/bin/sudo /bin/systemctl
reboot` or `poweroff`. Whether these commands work depends on sudo policy and
service identity. Granting them broad passwordless sudo is security-sensitive;
use the narrowest deployment-specific policy or disable exposure of remote
commands.

## Operational checks

Before enabling the service:

1. validate effective environment configuration;
2. confirm serial device names and permissions;
3. confirm the MAVLink bind address and firewall rules;
4. verify `usersettings.json` and `logs/` ownership;
5. exercise restart, reboot, and shutdown only in a controlled environment;
6. confirm RTCMv3 output and telemetry with the intended MAVLink client.

See [Configuration](configuration.md) and
[Known limitations](known-limitations.md).
