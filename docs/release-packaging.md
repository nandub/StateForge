# Release Packaging

StateForge v0.15.0 adds release-readiness packaging support.

## Build Packages

```powershell
.\scripts\Build-StateForgePackages.ps1 `
    -OutputPath .\artifacts\nuget `
    -Version 0.15.0
```

Packages currently targeted:

- StateForge.Core
- StateForge.FileStore
- StateForge.AspNet
- StateForge.AspNetCore
- StateForge.Security
- StateForge.Telemetry
- StateForge.CloudNative
- StateForge.Format

## Release Validation

```powershell
.\scripts\Test-StateForgeRelease.ps1 `
    -PackageOutputPath .\artifacts\nuget
```

This runs:

- solution build
- STFG2 format tests
- STFG2 envelope tests
- STFG2 migration tests
- STFG2 store migration tests
- Maintenance Host tests
- Scheduled Task helper tests
- package generation

## Maintenance Host Healthy Output

v0.15.0 changes maintenance host JSON output so `healthy` is `null` when health was not executed.

Examples:

Health-only:

```json
{
  "healthRan": true,
  "healthy": true
}
```

Stats-only:

```json
{
  "healthRan": false,
  "healthy": null
}
```

This avoids monitoring systems misreading skipped health checks as failures.

## v0.15.1

Adds NuGet README metadata, license metadata, repository metadata, package tags, symbol package settings, and package metadata validation.
