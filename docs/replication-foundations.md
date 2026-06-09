# Distributed Replication Foundations

StateForge v0.21.0 introduces distributed replication foundations.

## Purpose

This release does not implement a full consensus system. It adds a safe file-level replication foundation that can be used to validate farm replication behavior.

## Included Components

- `StateForge.Replication`
- `StateForgeFileReplicator`
- `StateForgeReplicationPlanner`
- `StateForgeReplicationHealth`
- `StateForge.ReplicationTests`

## Supported v0.21.0 Scenario

```text
Primary StateForge store
    -> Replica A
    -> Replica B
```

The replicator preserves the existing directory layout, including sharded session paths.

## Validate

```powershell
.\scripts\Test-StateForgeReplication.ps1
```

## Design Constraints

v0.21.0 is intentionally conservative:

- no automatic conflict resolution
- no distributed lock manager
- no consensus protocol
- no bidirectional sync
- no background daemon

Those belong in later versions.

## Future Roadmap

### v0.21.x

- replication manifests
- dry-run summaries
- conflict detection
- last-write metadata checks

### v0.22.x

- scheduled replication host
- replication health endpoint
- Prometheus replication metrics

### v0.23.x

- active/passive farm validation
- failover validation
