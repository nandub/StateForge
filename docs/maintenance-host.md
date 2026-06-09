# Maintenance Host

StateForge v0.14.0 adds the first service-style Maintenance Host.

## Run Once

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath ..\StateForgeSmoke\demo -Once
```

## JSON Output

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath ..\StateForgeSmoke\demo -Once -Json
```

## Loop Mode

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath D:\StateForge -Loop -IntervalSeconds 900
```

## Config File

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -Config .\config\stateforge-maintenance.sample.json -Once
```

## Scheduled Task

```powershell
.\scripts\Register-StateForgeMaintenanceTask.ps1 -RootPath D:\StateForge -FrequencyMinutes 15 -WhatIf
```

```powershell
.\scripts\Unregister-StateForgeMaintenanceTask.ps1 -WhatIf
```

## Test

```powershell
.\scripts\Test-StateForgeMaintenanceHost.ps1
```

## Scope

This release supports once mode, loop mode, JSON output, config files, optional STFG2 migration job execution, and Scheduled Task helpers. Native Windows Service installation is planned later.


## v0.14.1 Hardening

### Explicit Jobs

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath D:\StateForge -CleanupOnly -Once
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath D:\StateForge -HealthOnly -Once -Json
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath D:\StateForge -StatsOnly -Once -Json
.\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath D:\StateForge -MigrationOnly -Once
```

### Config Validation

```powershell
.\scripts\Test-StateForgeMaintenanceConfig.ps1 -Config .\config\stateforge-maintenance.sample.json
```

### Scheduled Task Helper Validation

```powershell
.\scripts\Test-StateForgeMaintenanceTask.ps1 -RootPath ..\StateForgeSmoke\demo
```

### Log Rotation

Config fields:

```json
{
  "maxLogSizeMb": 50,
  "maxLogFiles": 10
}
```


## v0.15.0 Healthy Output Change

When health is not executed, JSON output now reports:

```json
"healthRan": false,
"healthy": null
```

This avoids false unhealthy alerts during cleanup-only or stats-only runs.
