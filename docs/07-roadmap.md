# Roadmap

This roadmap reflects the current StateForge direction after the v1.0.1 hardening release.

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
| v0.35.0 | Site metadata, cross-site recovery policy, restore drills, and fenced site failover |
| v1.0.0 | Production release: soak evidence, package validation, deployment validation, performance gate, and recovery flow |
| v1.0.1 | Remote gRPC/TLS store client and host with required bearer authentication, separate admin token support, and lock-fencing hardening |

## Current Stable Release

`v1.0.1` is the current stable production release baseline. It supersedes the initial `v1.0.0` tag as
the recommended first install target.

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
- multi-site disaster recovery
- snapshots
- incremental snapshots
- replica promotion
- automatic failover
- recovery flow
- package metadata
- package symbols and SourceLink mappings
- package installation on `net8.0` and `net481`
- reviewed public API compatibility baselines
- rolling-upgrade and migration compatibility
- authenticated AES record and key-ring security validation
- remote gRPC/TLS store validation with required data-plane bearer authentication and separate admin token support
- long-duration soak evidence with final replication and snapshot

## Completed v1 Readiness Gates

- package metadata and repository identity review
- deterministic portable PDB generation
- SourceLink mapping and repository commit validation
- isolated local-feed package installation checks
- deterministic public API inventory for all twelve packages
- exact API drift validation across `net8.0` and `net481`
- same-layout mixed-version STFG1 read/write compatibility
- shard transition, replication, and snapshot upgrade checks
- documented AES and STFG2 downgrade boundaries
- record-level HMAC authentication and tamper rejection for new AES records
- bounded compressed-payload expansion and atomic validated key-ring saves
- reviewed small, medium, and large performance baselines
- automated broad-threshold performance regression validation
- configurable soak-test harness with cleanup, replication, snapshot, and final-verification coverage
- release-evidence disaster-recovery drill documentation
- reviewed 6-hour soak run with 1,000,000 operations, zero errors, final replication, and final snapshot
- Production suite validation through package, deployment, performance, snapshot, and recovery-flow checks

## v1.0.0 — Production Release

Completed release gates:

- reviewed long-duration soak-test run on production-like storage
- documented disaster-recovery drill evidence checklist
- package, deployment, performance, snapshot, and recovery-flow validation

## v1.0.1 — Remote Store Hardening Release

Completed release gates:

- remote gRPC/TLS client and host documentation, package metadata, and API docs
- required bearer authentication for remote data-plane calls
- distinct admin-token authorization for diagnostics, enumeration, cleanup, force-remove, stats, validation, and health RPCs
- fixed-time bearer-token comparison
- `SetAndUnlock` protection against recreating missing records with stale lock tokens

## Post-1.0 Roadmap

Define post-1.0 roadmap items after the hardening release is tagged and published.
