# ASV Drones GBS

Ground base station service with u-blox Real-Time Kinematic (RTK) support and
an ASV Drones desktop plugin for configuration and telemetry.

The service publishes MAVLink heartbeat, parameters, diagnostics, GBS state,
GNSS position, satellite counts, and RTCMv3 corrections. The plugin adds GBS
flight-mode actions, map integration, telemetry, and saved fixed-base
coordinates to an ASV Drones host.

## Solution

All projects target .NET 10 (`net10.0`). The solution is
`src/Asv.Drones.Gbs.sln` and contains four product projects plus tests.

| Project | Purpose |
| --- | --- |
| `Asv.Drones.Gbs` | Headless MAVLink/RTK service |
| `Asv.Drones.Gbs.Contracts` | Shared and generated MAVLink board parameters |
| `Asv.Drones.Plugin.Gbs` | ASV Drones desktop plugin |
| `Asv.Drones.Plugin.Gbs.App` | Standalone developer host for the plugin |
| `Asv.Drones.Gbs.Tests` | Baseline unit and documentation consistency tests |

## Quick start

Install the .NET 10 SDK, then run these commands from the repository root:

```bash
dotnet restore src/Asv.Drones.Gbs.sln
dotnet build src/Asv.Drones.Gbs.sln
dotnet test src/Asv.Drones.Gbs.sln
```

Run the headless service in its Debug default (`Virtual`) environment:

```bash
dotnet run --project src/Asv.Drones.Gbs/Asv.Drones.Gbs.csproj
```

Run the standalone plugin developer app:

```bash
dotnet run --project src/Asv.Drones.Plugin.Gbs.App/Asv.Drones.Plugin.Gbs.App.csproj
```

Set `DOTNET_ENVIRONMENT=Development`, `Virtual`, or `Production` to select an
explicit service environment. Review the configuration and deployment guides
before connecting hardware or exposing a production MAVLink listener.

## Packages and releases

Tag-triggered GitHub Actions package
`Asv.Drones.Gbs.Contracts` and `Asv.Drones.Plugin.Gbs`:

- `plugin-v<version>-dev[.<n>]` publishes to GitHub Packages;
- `plugin-v<version>-rc[.<n>]` publishes to GitHub Packages and NuGet.org;
- `plugin-v<version>` publishes to GitHub Packages and NuGet.org.

The tag version after `plugin-v` must exactly match `ProductVersion` in
`src/Directory.Build.props`. See the [release guide](docs/releases.md) for the
complete contract.

## Documentation

| Guide | Contents |
| --- | --- |
| [Development](docs/development.md) | Prerequisites, local build, run, test, and formatting commands |
| [Architecture](docs/architecture.md) | Projects, service startup, data flows, and external boundaries |
| [Configuration](docs/configuration.md) | Environments, options, user settings, connections, and logging |
| [Linux deployment](docs/deployment-linux.md) | Publish layout and deployment responsibilities |
| [GBS plugin](docs/plugin.md) | Registration, modes, actions, telemetry, and saved coordinates |
| [Board parameters](docs/parameters.md) | JSON/T4 generation and parameter consumers |
| [Releases](docs/releases.md) | Version/tag validation and package destinations |
| [Known limitations](docs/known-limitations.md) | Confirmed gaps and external unknowns |

Additional project context is maintained in
[DESCRIPTION.md](.ai-factory/DESCRIPTION.md) and
[ARCHITECTURE.md](.ai-factory/ARCHITECTURE.md).

## License

See [LICENSE](LICENSE).
