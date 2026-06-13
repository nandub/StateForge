# Production Readiness

StateForge v0.29.0 introduces a production-readiness validation layer.

## Production Suite

Run:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

This suite validates:

- documentation shape
- version consistency
- repository layout
- source guards
- health checks
- smoke tests
- observability
- replication
- snapshots
- recovery flow
- package metadata

## Purpose

The production suite is not a replacement for full release validation. It is a practical pre-flight check before promoting StateForge builds to a production-like environment.

## Recommended Order

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForge.ps1 -Suite Production
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages
```

## Dedicated Operational Scripts

Continue to use direct scripts for parameter-heavy operations:

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1
.\scripts\Start-StateForgeReplicationHost.ps1
.\scripts\New-StateForgeSnapshot.ps1
.\scripts\New-StateForgeIncrementalSnapshot.ps1
```
