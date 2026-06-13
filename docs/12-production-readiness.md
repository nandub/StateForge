# Production Readiness

StateForge v0.33.0 includes witness health and vote validation in the production-readiness layer.

## Production Suite

Run:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

This suite validates:

- documentation shape
- version consistency
- replica lag monitoring
- quorum and promotion eligibility policy
- witness heartbeat and candidate vote validation
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


## Non-Interactive Execution

Production validation is designed to run unattended. The suite provides a temporary health-check root automatically and does not prompt for `RootPath`.

Default health-check root:

```text
%TEMP%\StateForgeProductionHealth
```

This allows the suite to run in CI/CD, scheduled jobs, and release gates.


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.

## Replica Monitoring

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
```

The suite validates atomic and concurrent state updates, strict corrupt-state handling, stale-threshold
boundaries, named multi-replica configuration, dashboard reporting, and Prometheus output.

## Quorum Foundations

```powershell
.\scripts\Test-StateForge.ps1 -Suite Quorum
```

The suite validates majority and explicit thresholds, unavailable voters, candidate voting requirements,
replica-role checks, invalid configurations, and the no-election boundary.

## Witness Nodes

```powershell
.\scripts\Test-StateForge.ps1 -Suite Witness
```

The suite validates atomic state, strict corrupt-state handling, heartbeat freshness, identity matching,
candidate-specific votes, quorum restoration, and witness promotion rejection.
