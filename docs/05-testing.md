# Testing and Validation

## Build

```powershell
.\scripts\Build-StateForge.ps1
```

## Documentation

```powershell
.\scripts\Test-StateForgeDocs.ps1
```

## Version Consistency

```powershell
.\scripts\Test-StateForgeVersionConsistency.ps1
```

## Core Format

```powershell
.\scripts\Test-StateForgeFormat.ps1
.\scripts\Test-StateForgeStfg2Envelope.ps1
.\scripts\Test-StateForgeStfg2Migration.ps1
.\scripts\Test-StateForgeStfg2StoreMigration.ps1
```

## Operations

```powershell
.\scripts\Test-StateForgeMaintenanceHost.ps1
.\scripts\Test-StateForgeObservability.ps1
```

## High Availability / Disaster Recovery

```powershell
.\scripts\Test-StateForgeShardingImplementation.ps1
.\scripts\Test-StateForgeReplication.ps1
.\scripts\Test-StateForgeReplicationService.ps1
.\scripts\Test-StateForgeReplicaMonitoring.ps1
.\scripts\Test-StateForgeQuorum.ps1
.\scripts\Test-StateForgeSnapshotServices.ps1
.\scripts\Test-StateForgeAutomaticFailover.ps1
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
.\scripts\Test-StateForgeRecoveryFlow.ps1
```

## Release Hardening

```powershell
.\scripts\Test-StateForgeHardening.ps1
.\scripts\Test-StateForgeRelease.ps1
```


## Consolidated Runner

StateForge uses a single suite-based validation runner:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Version
.\scripts\Test-StateForge.ps1 -Suite Format
.\scripts\Test-StateForge.ps1 -Suite Migration
.\scripts\Test-StateForge.ps1 -Suite Observability
.\scripts\Test-StateForge.ps1 -Suite Maintenance
.\scripts\Test-StateForge.ps1 -Suite Replication
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
.\scripts\Test-StateForge.ps1 -Suite Quorum
.\scripts\Test-StateForge.ps1 -Suite Snapshots
.\scripts\Test-StateForge.ps1 -Suite Recovery
.\scripts\Test-StateForge.ps1 -Suite Release
.\scripts\Test-StateForge.ps1 -Suite All
```

Feature-specific scripts remain available for compatibility.


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


## Production Non-Interactive Guard

```powershell
.\scripts\Test-StateForgeProductionNonInteractive.ps1
```

This verifies that production validation provides a default `RootPath` to health validation.


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.
