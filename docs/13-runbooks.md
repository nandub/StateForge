# Production Runbooks

This document provides concise operator runbooks for StateForge production-like operations.

## Failover Drill

1. Verify replica health.
2. Confirm latest snapshot or replica state.
3. Run recovery validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Recovery
```

4. Promote the target replica using the dedicated promotion/failover tooling.
5. Validate application traffic against the promoted store.

## Snapshot Restore Drill

1. Select the snapshot chain.
2. Restore base snapshot.
3. Apply incrementals.
4. Validate restored sessions.
5. Record restore duration.

Validation:

```powershell
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
.\scripts\Test-StateForgeRecoveryFlow.ps1
```

## Rolling Upgrade Check

1. Confirm package versions:

```powershell
.\scripts\Test-StateForgeVersionConsistency.ps1
```

2. Run smoke validation.
3. Run production suite.
4. Upgrade one app node.
5. Verify read/write compatibility.
6. Continue rollout.

## Replica Loss Simulation

1. Stop or disconnect a replica.
2. Run replication validation.
3. Restore replica from snapshot or fresh fanout.
4. Validate recovery flow.

```powershell
.\scripts\Test-StateForge.ps1 -Suite Replication
.\scripts\Test-StateForge.ps1 -Suite Recovery
```

## Package Verification

```powershell
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages
.\scripts\Test-StateForgePackageMetadata.ps1
```


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.

## Replica Lag Check

1. Confirm `stateforge-replica-state.json` exists in each replica root.
2. Review `lastSuccessfulSyncUtc`, `failedSyncs`, and `lastError`.
3. Scrape `/stateforge/prometheus` with `STATEFORGE_REPLICA_ROOTS` configured.
4. Alert when `stateforge_replica_healthy` is `0` or lag exceeds the operating threshold.
5. Run catch-up before promotion if a replica is stale.

Validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
```
