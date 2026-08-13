# Releases and packages

## Version source

`src/Directory.Build.props` owns `ProductVersion`. The contracts and plugin
packages use this value, and release workflows compare it with the tag after
removing the `plugin-v` prefix. The values must match exactly, including any
prerelease suffix.

Example for `ProductVersion=2.1.0`:

```text
plugin-v2.1.0
```

Example for `ProductVersion=2.1.0-dev.3`:

```text
plugin-v2.1.0-dev.3
```

## Accepted tag contracts

| Channel | Accepted tag forms | Destinations |
| --- | --- | --- |
| Development | `plugin-vX.Y.Z-dev`, `plugin-vX.Y.Z-dev.N` | GitHub Packages |
| Release candidate | `plugin-vX.Y.Z-rc`, `plugin-vX.Y.Z-rc.N` | GitHub Packages and NuGet.org |
| Stable | `plugin-vX.Y.Z` | GitHub Packages and NuGet.org |

`X`, `Y`, `Z`, and `N` are decimal integers. Other tag forms do not trigger the
committed workflows.

## Workflow behavior

`.github/workflows/deploy-plugin-dev-nuget.yml` handles development tags.
`.github/workflows/deploy-plugin-nuget.yml` handles release-candidate and stable
tags. Both workflows:

1. check out the tagged commit;
2. install a .NET 10 SDK;
3. derive `VERSION` from the tag;
4. read `ProductVersion` from `src/Directory.Build.props`;
5. fail when those versions differ;
6. restore and build the plugin and dependencies in Release;
7. pack `Asv.Drones.Gbs.Contracts` and `Asv.Drones.Plugin.Gbs`;
8. push `.nupkg` files with duplicate skipping.

The headless `Asv.Drones.Gbs` executable and standalone developer app are not
published by these NuGet workflows.

## Required secrets and sources

Both workflows add `https://nuget.pkg.github.com/asv-soft/index.json` and use
`USER_NAME` plus `GIHUB_NUGET_AUTH_TOKEN` (the spelling is the current workflow
contract). The stable/RC workflow additionally uses `NUGET_AUTH_TOKEN` for
`https://api.nuget.org/v3/index.json`.

Repository or organization administrators own secret provisioning. Do not put
tokens in project files, command history, documentation examples, or commits.

## Release checklist

1. Update `ProductVersion` to the exact intended package version.
2. Run restore, Release build, formatting check, documentation checks, and tests.
3. Review generated parameter output and package contents.
4. Commit and merge the version change.
5. Create an accepted tag that exactly matches `ProductVersion` after
   `plugin-v`.
6. Monitor package publication to every destination for that channel.

The tag workflows do not run on ordinary pushes or pull requests. Pull-request
validation is provided by the separate CI workflow.
