# Solution Layout

## Core Platform

| Project | Purpose |
|---|---|
| `StateForge.Core` | Shared abstractions and core models |
| `StateForge.Format` | STFG/STFG2 file format |
| `StateForge.Security` | Security and encryption helpers |
| `StateForge.FileStore` | File-backed state/session store |
| `StateForge.Telemetry` | Runtime metrics |
| `StateForge.Prometheus` | Prometheus text output |
| `StateForge.Snapshots` | Snapshot, promotion, and failover services |
| `StateForge.CloudNative` | Cloud-native helpers |

## Providers

| Project | Purpose |
|---|---|
| `StateForge.AspNet` | ASP.NET Framework integration |
| `StateForge.AspNetCore` | ASP.NET Core integration |
| `StateForge.Telemetry.AspNetCore` | ASP.NET Core telemetry endpoints |

## Operations

| Project | Purpose |
|---|---|
| `StateForge.Maintenance` | Cleanup, stats, and health operations |
| `StateForge.Maintenance.Host` | Maintenance host executable |
| `StateForge.Tools` | CLI-style tooling |
| `StateForge.Replication` | Replication library |
| `StateForge.Replication.Host` | Replication host executable |
| `StateForge.ReplicaMonitoringTests` | Replica lag, stale-state, and Prometheus validation |
| `StateForge.QuorumTests` | Cluster membership, quorum, and promotion eligibility validation |
| `StateForge.WitnessTests` | Witness persistence, health, vote, and quorum validation |
| `StateForge.SplitBrainTests` | Primary lease, promotion fencing, and failover safety validation |
| `StateForge.MultiSiteTests` | Site metadata, cross-site policy, restore drill, and fenced failover validation |

## Harnesses and Tests

The repository uses executable harness projects for validation. Examples include:

- `StateForge.FormatHarness`
- `StateForge.MigrationHarness`
- `StateForge.StoreMigrationHarness`
- `StateForge.KestrelHarness`
- `StateForge.AspNetHarness`
- `StateForge.ReplicationTests`
- `StateForge.ReplicationHostTests`
- `StateForge.ReplicaMonitoringTests`
- `StateForge.QuorumTests`
- `StateForge.WitnessTests`
- `StateForge.SplitBrainTests`
- `StateForge.MultiSiteTests`
- `StateForge.ApiCompatibilityTests`
- `StateForge.UpgradeCompatibilityTests`
- `StateForge.SecurityTests`
- `StateForge.PackageValidationTests`
- `StateForge.SnapshotServiceTests`
- `StateForge.IncrementalSnapshotTests`
- `StateForge.RecoveryFlowTests`


## Script Consolidation

Top-level script entry points:

| Script | Purpose |
|---|---|
| `Build-StateForge.ps1` | Build validation |
| `Test-StateForge.ps1` | Consolidated validation suite runner |
| `Invoke-StateForge.ps1` | Consolidated operational command runner |
| `Build-StateForgePackages.ps1` | Package creation |
| `Test-StateForgeApiCompatibility.ps1` | Reviewed public API baseline validation |
| `Test-StateForgeUpgradeCompatibility.ps1` | Mixed-version store and recovery compatibility |
| `Test-StateForgeSecurity.ps1` | AES record integrity and key-ring persistence validation |
| `Test-StateForgePackages.ps1` | Package metadata, SourceLink, artifact, and install validation |

Reviewed package API signatures are stored as one text file per package under `api-baselines`.


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
