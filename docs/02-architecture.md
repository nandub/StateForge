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

## Snapshots

Snapshots copy session files into a repository. Incremental snapshots add delta manifests containing `add`, `modify`, and `delete` entries.

## Failover

Failover evaluates primary health, selects a replica, promotes it into a new primary root, and writes marker files.
