# Disaster Recovery

StateForge disaster recovery is built from snapshots, incremental deltas, promotion, and failover.

## Full Snapshots

A full snapshot copies the complete `sessions` directory and writes a manifest.

## Incremental Snapshots

Incremental snapshots compare a current store to a parent snapshot and write only changed files plus delete markers.

Delta actions:

| Action | Meaning |
|---|---|
| `add` | New file exists in source but not parent |
| `modify` | File exists in both but changed |
| `delete` | File existed in parent but no longer exists |

Validation:

```powershell
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
```

## Replica Promotion

Replica promotion restores a replica or snapshot into a new primary root and writes a promotion marker.

## Automatic Failover

Automatic failover checks primary health, selects a usable replica, promotes it, and writes a failover marker.

## Recovery Flow

Validated end-to-end flow:

```text
replication -> snapshot -> restore -> promotion -> failover
```

Validation:

```powershell
.\scripts\Test-StateForgeRecoveryFlow.ps1
```
