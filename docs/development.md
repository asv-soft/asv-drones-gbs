# Development

## Prerequisites

- .NET 10 SDK.
- Git.
- A C# IDE such as JetBrains Rider or Visual Studio.
- Access to the configured NuGet sources for ASV packages.
- Optional: a sibling `asv-drones` checkout when developing against local ASV
  Drones projects.

All commands below run from the repository root.

## Restore and build

Restore local tools and solution dependencies:

```bash
dotnet tool restore
dotnet restore src/Asv.Drones.Gbs.sln
```

Build Debug or Release:

```bash
dotnet build src/Asv.Drones.Gbs.sln
dotnet build src/Asv.Drones.Gbs.sln --configuration Release
```

The solution contains the service, contracts, plugin, standalone plugin app, and
test project. Every project targets `net10.0`; shared versions are in
`src/Directory.Build.props`.

## Run

Run the headless service:

```bash
dotnet run --project src/Asv.Drones.Gbs/Asv.Drones.Gbs.csproj
```

In a Debug build, absence of `DOTNET_ENVIRONMENT` selects `Virtual`. Select an
environment explicitly when needed:

```bash
DOTNET_ENVIRONMENT=Development dotnet run --project src/Asv.Drones.Gbs/Asv.Drones.Gbs.csproj
```

Run the standalone plugin developer app:

```bash
dotnet run --project src/Asv.Drones.Plugin.Gbs.App/Asv.Drones.Plugin.Gbs.App.csproj
```

To compile the plugin against sibling ASV Drones source instead of packages,
place the repositories in the layout expected by the project references and
pass `-p:UseLocalAsvDrones=true`. The checked-in Rider configurations assume
that sibling layout.

## Test and formatting

```bash
dotnet test src/Asv.Drones.Gbs.sln --configuration Release
dotnet csharpier check .
```

The Husky pre-commit `check` group runs the same CSharpier check. To apply
formatting locally:

```bash
dotnet csharpier format .
```

The baseline tests cover generated parameter metadata, plugin value helpers,
configuration serialization, and effective service option binding. UI
integration tests are deferred; see [Known limitations](known-limitations.md).

## Parameter generation

Building the contracts project executes the T4 template used for tracked
`MavParams.cs` output. Edit the JSON source or template, never the generated
file. See [Board parameters](parameters.md) for the complete workflow.

## Common working directories

The service uses relative paths for `usersettings.json` and `logs/`. `dotnet
run` uses the project/run configuration working directory chosen by the CLI or
IDE, while a published deployment should set an explicit working directory.
The standalone app has its own `user_settings.json` and logs configuration; it
does not share the headless service file automatically.
