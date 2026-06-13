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
```


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
