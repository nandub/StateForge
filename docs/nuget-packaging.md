# NuGet Packaging

StateForge v0.15.1 improves package metadata and package artifact generation.

## Validate Package Metadata

```powershell
.\scripts\Test-StateForgePackageMetadata.ps1
```

## Build Packages

```powershell
.\scripts\Build-StateForgePackages.ps1 `
    -OutputPath .\artifacts\nuget `
    -Version 0.15.1
```

Expected artifacts:

```text
*.nupkg
*.snupkg
```

## Packages

- StateForge.Core
- StateForge.FileStore
- StateForge.AspNet
- StateForge.AspNetCore
- StateForge.Security
- StateForge.Telemetry
- StateForge.CloudNative
- StateForge.Format
