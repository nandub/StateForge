# Observability

StateForge includes telemetry counters, Prometheus output, and snapshot-backed metrics.

## Telemetry

Tracked areas include:

- reads
- writes
- deletes
- lock acquisitions
- lock contentions
- cleanups
- quarantines
- corruptions

## Prometheus

Prometheus text output is provided by `StateForge.Prometheus`.

Validation:

```powershell
.\scripts\Test-StateForgeObservability.ps1
```

## Snapshot-Backed Metrics

Snapshot-backed metrics allow the system to expose operational state without constantly scanning the store.

## Replica Lag Monitoring

Successful replication and catch-up operations update `stateforge-replica-state.json` in each replica root.
The file is operational metadata and is not stored under `sessions`.

Replica monitoring reports:

- last replication attempt
- last successful sync
- lag in seconds
- stale and healthy status
- catch-up operation count
- failed sync count

The stale threshold is configurable by callers. A replica with missing state, an expired successful-sync
timestamp, or a current sync error is unhealthy.

Prometheus metrics:

```text
stateforge_replica_lag_seconds
stateforge_replica_healthy
stateforge_replica_last_sync_timestamp
stateforge_replica_catchup_operations_total
stateforge_replica_failed_syncs_total
```

Kestrel appends these metrics to `/stateforge/prometheus` when
`STATEFORGE_REPLICA_ROOTS` contains semicolon-separated replica roots.
`STATEFORGE_REPLICA_STALE_SECONDS` sets the stale threshold and defaults to `300`.

Validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
```
