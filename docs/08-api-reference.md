# API Reference

This is a human-maintained API index. It should remain concise.

## File Store

- `StateForgeFileStore`
- `StateForgeFileStoreOptions`
- `StateForgeStoreStats`

Lock IDs returned by `GetAndLock` are fencing tokens. `SetAndUnlock` succeeds for an existing entry only
when the entry is actively locked with the supplied lock ID. `Refresh` returns `false` for expired entries.

## ASP.NET Core Cache

- `StateForgeDistributedCache`
- `StateForgeDistributedCacheOptions`

The cache adapter preserves sliding expiration and absolute expiration independently. Refresh extends the
sliding window without extending an absolute expiration deadline.

## Replication

- `StateForgeFileReplicator`
- `StateForgeReplicationOptions`
- `StateForgeReplicationResult`
- `StateForgeReplicationManifest`
- `StateForgeReplicaSyncState`
- `StateForgeReplicaStateStore`
- `StateForgeReplicaConfiguration`
- `StateForgeReplicaMonitor`
- `StateForgeReplicaMonitorSnapshot`
- `StateForgeReplicaMonitorEntry`

`StateForgeReplicaMonitor.Capture` accepts a stale threshold and can accept an explicit capture timestamp
for deterministic evaluation. Missing or stale state is reported as unhealthy.
`StateForgeReplicaConfiguration.Parse` accepts semicolon-separated `name=path` or positional path entries.
`StateForgeReplicaStateStore.Read` throws `InvalidDataException` for incomplete or invalid persisted state.

## Quorum

- `StateForgeClusterMember`
- `StateForgeClusterMemberRole`
- `StateForgeQuorumPolicy`
- `StateForgeQuorumEvaluator`
- `StateForgeQuorumResult`

`StateForgeQuorumEvaluator.Evaluate` calculates available and required votes and evaluates one explicitly
named promotion candidate. A zero `MinimumVotes` uses a strict majority of enabled voting members.
The evaluator does not select candidates, elect leaders, or invoke promotion.

## Witness Nodes

- `StateForgeWitnessNode`
- `StateForgeWitnessState`
- `StateForgeWitnessStateStore`
- `StateForgeWitnessHealthEntry`
- `StateForgeWitnessEvaluator`

`StateForgeWitnessStateStore` persists atomic `stateforge-witness-state.json` files.
`StateForgeWitnessEvaluator.Evaluate` validates heartbeat freshness, witness identity, errors, and a
candidate-specific granted vote. `ToClusterMember` creates a non-promotable witness quorum member whose
availability reflects the validated vote.

## Split-Brain Prevention

- `StateForgePrimaryLease`
- `StateForgePrimaryLeaseStore`
- `StateForgePromotionFenceOptions`
- `StateForgePromotionFenceResult`
- `StateForgePromotionFenceService`

`StateForgePromotionFenceService.Acquire` requires an eligible quorum result for the exact candidate.
Lease acquisition uses machine-local serialization plus an exclusive shared-file lock. A stale takeover
increments the epoch; active-owner reacquisition and `Renew` require the exact lease ID.

## Snapshots

- `StateForgeSnapshotService`
- `StateForgeSnapshotOptions`
- `StateForgeSnapshotResult`
- `StateForgeIncrementalSnapshotService`
- `StateForgeIncrementalSnapshotOptions`
- `StateForgeIncrementalSnapshotResult`

## Promotion / Failover

- `StateForgeReplicaPromotionService`
- `StateForgeReplicaPromotionOptions`
- `StateForgeFailoverService`
- `StateForgeFailoverOptions`

Set `RequirePromotionFence` and provide `PromotionFence` to make lease acquisition mandatory. Rejected
fencing returns an error without restoring data or writing promotion/failover markers.

## Replica Prometheus

- `StateForgeReplicaPrometheusCollector`
- `StateForgeReplicaPrometheusFormatter`
