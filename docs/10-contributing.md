# Contributing

## Rules

- Keep changes small and versioned.
- Keep PowerShell compatible with Windows PowerShell 5.1.
- Keep validation scripts explicit.
- Add or update tests with each feature.
- Do not add one documentation file per feature unless necessary.
- Prefer updating the consolidated documentation files.

## Required Checks

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForgeDocs.ps1
.\scripts\Test-StateForgeVersionConsistency.ps1
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
.\scripts\Test-StateForge.ps1 -Suite Packages
```

Package changes must preserve the centralized SourceLink settings in `Directory.Build.targets` and
pass artifact plus local-feed install validation for both supported consumer target families.


## Operational Script Dispatcher

```powershell
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages
```


## Operational Script Guidance

`Invoke-StateForge.ps1` is a convenience runner for low-parameter commands only.

Use direct scripts for parameter-rich operations:

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1
.\scripts\Start-StateForgeReplicationHost.ps1
.\scripts\New-StateForgeIncrementalSnapshot.ps1
```


## Production Readiness

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

See `docs\12-production-readiness.md` and `docs\13-runbooks.md`.
