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
