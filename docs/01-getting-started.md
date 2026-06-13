# Getting Started

## Build

```powershell
.\scripts\Build-StateForge.ps1
```

## Basic Validation

```powershell
.\scripts\Test-StateForgeDocs.ps1
.\scripts\Test-StateForgeVersionConsistency.ps1
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
```

## Full Stabilization Validation

```powershell
.\scripts\Test-StateForgeHardening.ps1
.\scripts\Test-StateForgeRecoveryFlow.ps1
```

## Deployment Modes

| Mode | Purpose |
|---|---|
| Single-node FileStore | Durable local state/session storage |
| Sharded FileStore | Large stores with better directory distribution |
| ASP.NET Provider | ASP.NET Framework session integration |
| ASP.NET Core Provider | ASP.NET Core session/cache integration |
| Replicated Store | Primary-to-replica file fanout |
| Snapshot-enabled Store | Disaster recovery and rollback |
| Failover-enabled Store | Replica promotion and failover workflows |

## Operational Guidance

Recommended production shape:

```text
Application
  -> StateForge Provider
  -> Sharded FileStore
  -> Maintenance Host
  -> Replication Host
  -> Snapshot / Failover Services
```
