# Roadmap

This roadmap reflects the current StateForge direction after v0.30.4 hardening.

## Completed

| Version | Milestone |
|---|---|
| v0.19.x | Sharding |
| v0.20.x | Snapshot-backed metrics |
| v0.21.x | Replication foundations |
| v0.22.x | Replication services |
| v0.23.x | Snapshot services |
| v0.24.x | Snapshot scheduling |
| v0.25.x | Replica promotion |
| v0.26.x | Automatic failover and recovery hardening |
| v0.27.x | Incremental snapshots |
| v0.28.x | Documentation and script stabilization |
| v0.29.x | Production-readiness validation |
| v0.30.x | Replica catch-up and resynchronization foundations |

## Current Stable Candidate

`v0.30.4` is the current stable production-candidate baseline.

It validates:

- build
- docs
- version consistency
- layout
- source guards
- health
- smoke tests
- observability
- replication
- replication services
- replication manifests
- replica catch-up
- snapshots
- incremental snapshots
- replica promotion
- automatic failover
- recovery flow
- package metadata

## v0.31.0 — Replica Lag Monitoring

Goal: give operators visibility into replica freshness.

Planned capabilities:

- track last successful replication timestamp
- calculate replica lag duration
- detect stale replicas
- expose replica health state
- expose Prometheus metrics
- add validation tests
- include monitoring in Production suite

Suggested metrics:

```text
stateforge_replica_lag_seconds
stateforge_replica_healthy
stateforge_replica_last_sync_timestamp
stateforge_replica_catchup_operations_total
stateforge_replica_failed_syncs_total
```

Suggested validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
```

## v0.31.1 — Replica Monitoring Stabilization

Planned capabilities:

- deterministic metric fixtures
- stale replica threshold tests
- multi-replica lag reporting
- Prometheus output validation
- docs and runbook updates

## v0.32.0 — Quorum Foundations

Planned capabilities:

- cluster member model
- quorum policy model
- promotion eligibility checks
- quorum validation harness
- no automatic leader election yet

## v0.33.0 — Witness Nodes

Planned capabilities:

- witness state file/model
- witness health checks
- witness vote validation
- failover integration points

## v0.34.0 — Split-Brain Prevention

Planned capabilities:

- primary lease markers
- promotion fencing checks
- stale-primary detection
- failover safety validation
- operator runbook updates

## v0.35.0 — Multi-Site Disaster Recovery

Planned capabilities:

- site metadata
- cross-site replication policy
- site failover runbooks
- restore drills
- package/runtime validation

## v1.0.0 — Production Release

Required before v1.0:

- public API review
- package metadata review
- SourceLink/package validation
- long-duration soak tests
- rolling upgrade checks
- DR drill documentation
- migration guide
- security review
- performance baseline
