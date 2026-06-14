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

## Rolling Upgrade and Migration Guide

### Supported Mixed-Version Path

Rolling upgrades are supported when old and current nodes:

- use the STFG1 live-store record layout
- use the same `ShardDepth`
- use protection modes understood by every node
- share compatible store and encryption configuration

Validate before rollout:

```powershell
.\scripts\Test-StateForge.ps1 -Suite UpgradeCompatibility
.\scripts\Test-StateForge.ps1 -Suite Production
```

Upgrade procedure:

1. Record the current package version, `ShardDepth`, compression, and protection settings.
2. Confirm snapshots and replicas are healthy.
3. Upgrade one node without changing store-format, sharding, or encryption settings.
4. Verify reads, writes, refreshes, removes, metrics, and health checks.
5. Continue one node at a time.
6. Run Production validation after the final node.

### Shard-Depth Migration

Current nodes can read and remove records at shard depths 0, 1, and 2. Mixed writers at different shard
depths are not supported because two physical copies of one key can diverge.

1. Complete the package rollout with the original `ShardDepth`.
2. Drain all older writers.
3. Back up or snapshot the store.
4. Run the sharding migration workflow:

```powershell
.\scripts\Invoke-StateForgeShardingMigration.ps1
```

5. Change `ShardDepth` consistently on all nodes.
6. Run `UpgradeCompatibility`, smoke, and Production validation.

### Encryption Downgrade Boundary

Do not enable AES until every node can read AES records and has the same key material. Older readers that
do not understand `FlagAesEncrypted` cannot read those records. Rolling back after AES writes requires
restoring a pre-AES snapshot or using a reviewed conversion process.

Current writers add `FlagAuthenticated` and an HMAC-SHA256 trailer covering the complete serialized
record. Older AES-capable readers can read the encrypted payload but do not enforce the authentication
trailer. During a rolling upgrade, treat nodes running older packages as unable to detect record
tampering. Complete the rollout before relying on authenticated-record enforcement.

Security validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Security
```

Protect the store root and key-ring file with least-privilege filesystem permissions. Do not place the
AES key or key-ring file inside the replicated session root, snapshots, container image, source tree, or
logs. Rotate keys through `Rotate-StateForgeKeyRing.ps1`; invalid rings are rejected before the existing
file is atomically replaced.

### STFG2 Boundary

STFG2 utilities currently provide an offline envelope and migration format. `StateForgeFileStore` live
session records remain STFG1. Do not apply `StateForgeStfg2StoreMigrator` to an active FileStore root:
current and older live-store readers do not read STFG2-wrapped session records.

Use STFG2 conversion only for offline artifacts or an explicitly designed future cutover. Preserve the
`.stfg1.bak` files until the converted artifacts have been independently verified.

### Rollback

Rollback is supported while the rollout retains the same STFG1 layout, shard depth, and mutually
supported protection mode. Stop the rollout and restore the previous packages one node at a time.

Rollback is not supported after:

- writes at a new shard depth before older writers are drained
- AES records are written for readers without AES support
- live records are wrapped in STFG2

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
3. Configure stable names, for example `STATEFORGE_REPLICA_ROOTS=west=C:\replicas\west;east=C:\replicas\east`.
4. Scrape `/stateforge/prometheus`, or run the dashboard with the same `name=path` entries.
5. Alert when `stateforge_replica_healthy` is `0` or lag exceeds the operating threshold.
6. Treat `InvalidDataException` in the last error as corrupt or incomplete state and investigate the writer.
7. Run catch-up before promotion if a replica is stale.

Validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
```

## Quorum Eligibility Check

Before operator-driven promotion:

1. Build the configured `StateForgeClusterMember` set.
2. Mark unavailable or disabled members accurately.
3. Evaluate the intended replica by name with `StateForgeQuorumEvaluator`.
4. Require both `HasQuorum` and `CandidateEligible`.
5. Review `Reasons` before overriding policy.

The evaluator does not perform promotion or leader election.

Validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Quorum
```

## Witness Health and Vote Check

Before counting a witness vote:

1. Confirm `stateforge-witness-state.json` exists in the configured witness root.
2. Confirm the heartbeat is within the operating threshold.
3. Confirm the persisted witness name matches the configured witness.
4. Confirm the vote is granted for the exact intended replica candidate.
5. Convert the result with `StateForgeWitnessEvaluator.ToClusterMember`.
6. Re-evaluate quorum and review all reasons before operator-driven promotion.

Witness validation does not initiate failover.

```powershell
.\scripts\Test-StateForge.ps1 -Suite Witness
```

## Fenced Promotion and Failover

1. Place the lease root on storage visible to every promotion coordinator.
2. Evaluate quorum for the exact replica candidate.
3. Set `RequirePromotionFence = true`.
4. Populate `PromotionFence` with the shared root, cluster name, candidate, quorum result, and lease duration.
5. Reject the operation when fencing is not acquired; review `PromotionFence.Reasons`.
6. Persist the returned lease ID and epoch with the active-primary operating state.
7. Renew before `ExpiresUtc` using the exact cluster, primary, and lease ID.
8. Never force a rival promotion before the current lease expires.

An expired owner must not renew its old token. A successful takeover receives a new token and a higher
epoch. A blocked operation must not produce promotion or failover markers.

```powershell
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
```

## Multi-Site Disaster Recovery Drill

1. Persist `stateforge-site-state.json` for the primary and recovery sites.
2. Confirm site names and regions are unique and roles are correct.
3. Replicate to a named replica carrying the recovery site and region metadata.
4. Confirm the replication manifest includes the expected site and region.
5. Create a snapshot and restore it into an isolated drill root.
6. Verify session counts and application-readable state in the drill root.
7. Evaluate `StateForgeCrossSiteEvaluator` for the exact recovery replica candidate.
8. Require both `RequireCrossSitePolicy` and `RequirePromotionFence` during site failover.
9. Confirm the failover marker records source and target site names.
10. Record recovery-point age, lease epoch, and drill results in the operational change record.

Do not treat the policy evaluator as site election. Operators or an external orchestrator must select the
candidate explicitly, and the cross-site result must match the promoted replica root.

```powershell
.\scripts\Test-StateForge.ps1 -Suite MultiSite
```

## Kubernetes Deployment

1. Build and publish `stateforge-kestrel:0.35.0` to the target registry.
2. Replace the image reference in `deploy\k8s\deployment.yaml`.
3. Confirm a `ReadWriteMany` storage class and set `storageClassName` on the PVC if required.
4. Leave encryption disabled initially, or provision the AES secret before enabling it.
5. Render with `kubectl kustomize .\deploy\k8s`, then apply and wait for the Deployment rollout.
6. Confirm `/livez`, `/readyz`, and `/stateforge/prometheus`.
7. Confirm the mounted path is writable by UID/GID `1654`.
8. Confirm the HPA sees CPU metrics before relying on autoscaling.
9. Verify `/session/...` returns `404`; demo endpoints must remain disabled.

```powershell
.\scripts\Test-StateForge.ps1 -Suite Deployment
kubectl apply -k .\deploy\k8s
kubectl rollout status deployment/stateforge-kestrel
```
