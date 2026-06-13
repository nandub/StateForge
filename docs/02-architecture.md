# Architecture

StateForge is a file-backed state platform composed of small libraries and operational hosts.

## High-Level Flow

```text
Applications
  -> ASP.NET / ASP.NET Core providers
  -> StateForge FileStore
     -> STFG/STFG2 format
     -> Security envelope
     -> Sharded sessions
     -> Telemetry counters
     -> Replication
        -> Replica sync state
        -> Lag / stale evaluation
        -> Quorum policy evaluation
     -> Snapshots
     -> Promotion / Failover
```

## Storage

StateForge stores session entries as `.stfg` files under a `sessions` directory.

Core projects:

- `StateForge.Core`
- `StateForge.Format`
- `StateForge.Security`
- `StateForge.FileStore`

## Sharding

Sharding distributes session files into hash-derived folders. This avoids large flat directories and supports rolling migration through fallback reads.

## Replication

Replication performs primary-to-replica file fanout and can produce manifests. Current replication is deterministic and file-based, not consensus-based.

Each non-dry-run replication or catch-up attempt updates atomic operational metadata in the replica root.
`StateForgeReplicaMonitor` projects that state into lag, stale, and health results using a configurable threshold.

Quorum foundations model enabled voting members, available votes, majority or explicit thresholds, and
replica promotion eligibility. The evaluator is deterministic policy logic only; it does not elect a leader
or invoke failover.

## Snapshots

Snapshots copy session files into a repository. Incremental snapshots add delta manifests containing `add`, `modify`, and `delete` entries.

## Failover

Failover evaluates primary health, selects a replica, promotes it into a new primary root, and writes marker files.
