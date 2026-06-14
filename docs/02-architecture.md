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
        -> Witness health and votes
        -> Primary lease and fencing epoch
        -> Site metadata and cross-site policy
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

Witness nodes persist heartbeat and candidate-specific vote state outside session storage. A witness vote
counts only when the state is fresh, error-free, identity-matched, and granted for the evaluated candidate.
Witnesses participate in quorum but are never promotion candidates.

Primary leases are stored in a shared lease root outside session data. Promotion fencing combines an
eligible quorum result with an exclusive shared-file lock, an expiring owner token, and a monotonically
increasing epoch. A different candidate cannot acquire ownership until the active lease expires.

Multi-site recovery persists site identity, region, role, health, heartbeat, and recovery-point metadata
outside session data. Cross-site policy checks distinct sites, region separation, target freshness and
health, promotion eligibility, and quorum for the exact candidate.

## Snapshots

Snapshots copy session files into a repository. Incremental snapshots add delta manifests containing `add`, `modify`, and `delete` entries.

## Failover

Failover evaluates primary health, selects a replica, and can require promotion fencing before restoring
the new primary. Fenced rejection suppresses promotion and failover markers.
Cross-site failover can additionally require an eligible site policy result bound to the exact replica root.
