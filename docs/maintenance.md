# Maintenance

StateForge v0.12.0 adds the first maintenance utility.

## Scope

The maintenance utility can run:

- cleanup
- health check
- statistics
- all jobs

## Run All Jobs

```powershell
.\scripts\Invoke-StateForgeMaintenance.ps1 `
    -RootPath ..\StateForgeSmoke\demo `
    -Once all
```

## Cleanup Only

```powershell
.\scripts\Invoke-StateForgeMaintenance.ps1 `
    -RootPath D:\StateForge `
    -Once cleanup
```

## Health Only

```powershell
.\scripts\Invoke-StateForgeMaintenance.ps1 `
    -RootPath D:\StateForge `
    -Once health
```

## Stats Only

```powershell
.\scripts\Invoke-StateForgeMaintenance.ps1 `
    -RootPath D:\StateForge `
    -Once stats
```

## Future Work

Future maintenance releases should add:

- scheduled loop mode
- Windows Service hosting
- backup pruning
- quarantine pruning
- key migration jobs
- JSON output
- Windows Event Log integration
