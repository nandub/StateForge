# Roadmap

This roadmap reflects the current StateForge direction after v0.34.0 split-brain prevention.

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
| v0.31.0 | Replica lag monitoring |
| v0.31.1 | Replica monitoring stabilization and dashboard integration |
| v0.32.0 | Quorum foundations and promotion eligibility policy |
| v0.33.0 | Witness state, health, and quorum vote validation |
| v0.34.0 | Primary leases, promotion fencing, and stale-primary takeover |

## Current Stable Candidate

`v0.34.0` is the current stable production-candidate baseline.

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
- quorum policy
- witness health and votes
- split-brain prevention
- snapshots
- incremental snapshots
- replica promotion
- automatic failover
- recovery flow
- package metadata

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
